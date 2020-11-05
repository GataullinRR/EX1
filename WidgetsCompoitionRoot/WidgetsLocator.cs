using CalibrationWidget;
using Calibrators;
using CommandExports;
using CommandWidget;
using Common;
using DataRequestWidget;
using DataViewExports;
using DataViewWidget;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using Exporters.Las;
using ExportersExports;
using FilesExports;
using FilesWidget;
using FlashDumpLoaderWidget;
using FlashUploadWidget;
using InitializationWidget;
using MVVMUtilities.Types;
using RUSManagingToolExports;
using RUSModuleSetDirrectionWidget;
using RUSTelemetryStreamSenderWidget;
using StatusWidget;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using ViewSettingsWidget;

namespace WidgetsCompositionRoot
{
    public static class WidgetsLocator
    {
        class WidgetInfo
        {
            WeakReference<IWidget> _Widget;
            public IWidget Widget => _Widget.GetTargetOrDefault();
            public IRUSDevice Scope { get; }

            public WidgetInfo(IWidget widget, IRUSDevice scope)
            {
                _Widget = new WeakReference<IWidget>(widget ?? throw new ArgumentNullException(nameof(widget)));
                Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            }
        }

        readonly static List<WidgetInfo> _allWidgets = new List<WidgetInfo>();
        readonly static Dictionary<IRUSDevice, List<IDeviceHandler>> _models = new Dictionary<IRUSDevice, List<IDeviceHandler>>();

        public static IEnumerable<IWidget> AllWidgets => _allWidgets.Select(wr => wr?.Widget).SkipNulls().ToArray();

        public static IList<IDeviceHandler> GetDeviceHandler(IRUSDevice rusDevice)
        {
            return _models[rusDevice];
        }
        
        public static T Resolve<T>()
        {
            var t = typeof(T);
            if (t == typeof(IFileExtensionFactory))
            {
                return (T)FileExtensionFactory.Instance;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public static IWidget[] ResolveWidgetsForScope(IRUSDevice scopeDevice, BusyObject busy)
        {
            if (_models.ContainsKey(scopeDevice))
            {
                throw new InvalidOperationException("Only one set of widgets per scope is allowed");
            }

            using (var core = new DICore()) 
            {
                registerBasicServices();
                instantiateWidgets();

                var widgets = core.Container.ResolveAll<IWidget>().ToArray();
                _allWidgets.AddRange(widgets.Select(w => new WidgetInfo(w, scopeDevice)));
                _models[scopeDevice] = core.Container.TryResolveAll<IDeviceHandler>()?.ToList() ?? new List<IDeviceHandler>();

                return widgets;

                void registerBasicServices()
                {
                    core.Container.Register(scopeDevice);
                    core.Container.Register(scopeDevice.Name); // We're registering string. Dont be confused
                    core.Container.Register(busy);
                }

                void instantiateWidgets()
                {
                    var instantiator = core.InstantiationStrategy;
                    var instantiationCoroutines = widgetsFactory(core.Container).ToArray();
                    instantiator.ExecuteCoroutines(instantiationCoroutines);
                }
            }

            //////////////////////////////////////

            IEnumerable<IEnumerator<ResolutionStepResult>> widgetsFactory(IDIContainer container)
            {
                string dataViewScope = nameof(dataViewScope);
                string flashDumpViewScope = nameof(flashDumpViewScope);

                yield return injectExporters(container, dataViewScope);

                yield return DeviceInitialization.InstantiationCoroutine(dataViewScope, container);
                yield return DeviceFiles.InstantiationCoroutine(dataViewScope, container);
                yield return DeviceStatus.InstantiationCoroutine(dataViewScope, container);
                yield return DataRequest.InstantiationCoroutine(dataViewScope, container);
                yield return DataView.InstantiationCoroutine("Данные", dataViewScope, container);

                foreach (var command in getCommands(scopeDevice, busy))
                {
                    if (command == Command.DOWNLOAD_FLASH && Command.DOWNLOAD_FLASH.GetInfo().IsSupportedForDevice(scopeDevice.Id))
                    {
                        yield return FlashUploadCommand.InstantiationCoroutine(flashDumpViewScope, container);
                    }
                    yield return DeviceCommand.InstantiationCoroutine(command, dataViewScope, container);
                }

                foreach (var calibrator in getCalibrators(scopeDevice, new FileSaver(scopeDevice.Id, FileExtensionFactory.Instance).SaveCalibrationFileAsync, busy).ToArray())
                {
                    yield return DeviceCalibration.InstantiationCoroutine(calibrator, dataViewScope, container);
                    yield return injectCalibrationWidgets(container, calibrator);
                }

                foreach (var widget in deviceSpecificWidgetsFactory())
                {
                    yield return widget;
                }

                yield return DataViewSettings.InstantiationCoroutine(false, dataViewScope, container);

                IEnumerable<IEnumerator<ResolutionStepResult>> deviceSpecificWidgetsFactory()
                {
                    if (scopeDevice.Id.IsOneOf(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE, RUSDeviceId.EMC_MODULE))
                    {
                        yield return FlashDumpLoad.InstantiationCoroutine(flashDumpViewScope, container);
                        yield return DataView.InstantiationCoroutine("Данные дампа Flash", flashDumpViewScope, container);
                        yield return DataViewSettings.InstantiationCoroutine(true, flashDumpViewScope, container);
                        yield return injectExporters(container, flashDumpViewScope);
                    }
                    if (scopeDevice.Id.IsOneOf(RUSDeviceId.RUS_MODULE))
                    {
                        yield return RUSModuleSetDirection.InstantiationCoroutine(dataViewScope, container);
                        yield return RUSTelemetryStreamSender.InstantiationCoroutine(dataViewScope, container);
                    }
                }
            }
        }

        #region ### Factories ###

        static IEnumerator<ResolutionStepResult> injectCalibrationWidgets(IDIContainer container, ICalibrator calibrator)
        {
            //Skip(1) because the first item is calibration widget itself
            foreach (var calibrationDataWidget in calibrator.Widgets.Skip(1))
            {
                container.Register<IWidget>(calibrationDataWidget);
            }

            yield return ResolutionStepResult.RESOLVED;
        }

        static IEnumerator<ResolutionStepResult> injectExporters(IDIContainer container, object scope)
        {
            while (true)
            {
                var exporters = CommonUtils.TryOrNull<ICurvesExporterVM[], ServiceIsNotYetAwailableException>(() => instantiate().ToArray());
                if (exporters == null)
                {
                    yield return ResolutionStepResult.WAITING_FOR_SERVICE;
                }
                else
                {
                    foreach (var exporter in exporters)
                    {
                        container.Register<ICurvesExporterVM>(exporter, scope);    
                    }

                    yield return ResolutionStepResult.RESOLVED;
                }
            }

            IEnumerable<ICurvesExporterVM> instantiate()
            {
                var storage = container.ResolveSingle<IPointsStorageProvider>(scope);
                yield return new LasCurvesExporterVM(storage);
            }
        }

        static IEnumerable<Command> getCommands(IRUSDevice device, BusyObject isBusy)
        {
            return EnumUtils
                .GetValues<Command>()
                .Select(a => (Addr: a, Info: a.GetInfo()))
                .Where(i => i.Info != null
                         && i.Info.IsCommand
                         && i.Info.IsSupportedForDevice(device.Id))
                .Select(i => i.Addr);
        }

        static IEnumerable<ICalibrator> getCalibrators(
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync,
            BusyObject isBusy)
        {
            return CalibratorsFactory
                .GetCalibrators(device, isBusy, saveCalibrationFileAsync)
                .ToArray();
        }

        #endregion
    }
}
