using Common;
using DataViewExports;
using DeviceBase.Devices;
using DeviceBase.Models;
using FlashDumpLoaderExports;
using MVVMUtilities.Types;
using NetFrameworkUtilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using WPFUtilities.Types;
using System.Linq;

namespace FlashDumpLoaderWidget
{ 
    public class FlashDumpLoadVM : IPointsStorageProvider, IFlashDumpLoader
    {
        Task _currentOperationTask;
        AsyncOperationInfo _currentOperation;

        public event PropertyChangedEventHandler PropertyChanged;

        public BusyObject IsBusy { get; }
        public ActionCommand Load { get; }
        public ActionCommand Cancel { get; }
        public IDataPointsStorage PointsSource { get; private set; }
        public IDataStorageVM DataStorageVM { get; }

        public FlashDumpLoadVM(RUSDeviceId rusDevice, BusyObject isBusy)
        {
            IsBusy = isBusy;

            DataStorageVM = new FlashDataStorageVM(this);
            Load = new ActionCommand(loadAsync, IsBusy);
            Cancel = new ActionCommand(cancelAsync, () => _currentOperation != null, IsBusy);

            async Task loadAsync()
            {
                using (IsBusy.BusyMode)
                {
                    var path = IOUtils.RequestFileOpenPath("BIN (*.bin)|*.bin");
                    if (path == null)
                    {
                        return;
                    }

                    try
                    {
                        _currentOperation = new AsyncOperationInfo().UseInternalCancellationSource();
                        _currentOperationTask = LoadAsync(path, _currentOperation);
                        Cancel.Update();
                        await _currentOperationTask;

                        Logger.LogOKEverywhere("Дамп Flash успешно загружен");
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogInfoEverywhere("Чтение дампа Flash отменено");

                        PointsSource = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Ошибка загрузки дампа", $"-MSG", ex);

                        PointsSource = null;
                    }
                    finally
                    {
                        _currentOperationTask = null;
                        _currentOperation = null;
                    }
                }
            }

            async Task cancelAsync()
            {
                _currentOperation?.Cancel();
            }
        }

        public async Task LoadAsync(string dumpPath, AsyncOperationInfo operationInfo)
        {
            if (dumpPath != null)
            {
                await NetFrameworkUtils.ContinueAtUIThread();

                var dump = await FlashDump.OpenAsync(dumpPath, operationInfo);
                var dataParser = new SectionedDataPacketParser(
                    dump.DataRowDescriptors.Select(ds => new SectionedDataPacketParser.Section(
                        ds.Key, 
                        new DataPacketParser(ds.Value))).ToArray());
                var dumpReader = await FlashDumpDataParser.CreateParserAsync(dump, dataParser, operationInfo);
                var rowsReader = await dumpReader.InstantiateReaderAsync(operationInfo);
                var rowsCollection = new FileMappedPointsRowsReadOnlyCollection(rowsReader);
                PointsSource = new DataPointsSource(new EnhancedObservableCollection<ICurveInfo>(dataParser.Curves, new CollectionOptions(true)), rowsCollection);
            }
        }
    }
}
