using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase;
using Calibrators.ViewModels;
using MVVMUtilities.Types;
using Calibrators.Views;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using Calibrators.ViewModels.Inclinometr;
using Common;

namespace Calibrators
{
    public static class CalibratorsFactory
    {
        public static IEnumerable<ICalibrator> GetCalibrators(
            IRUSDevice device, 
            BusyObject uiBusy, 
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
        {
            var scope = device.Id.GetEnumValueDescription();

            switch (device.Id)
            {
                case RUSDeviceId.INCLINOMETR:
                    {
                        var vm = new TemperatureCalibrationVM(uiBusy, device, saveCalibrationFileAsync);
                        var view = new InclinometrTemperatureCalibration()
                        {
                            FunctionId = new WidgetIdentity(vm.CalibrationName, scope, null),
                            ViewModel = vm
                        };
                        var errorWidget = new InclinometrTemperatureCalibrationErrors()
                        {
                            FunctionId = new WidgetIdentity("Температурные отклонения", scope, null),
                            ViewModel = new TemperatureCalibrationErrorsVM(vm.Calibrator)
                        };

                        yield return new Calibrator(vm, view, errorWidget);
                    }
                    {
                        var vm = new AngularCalibrationVM(uiBusy, device, saveCalibrationFileAsync);
                        var view = new InclinometrAngularCalibration()
                        {
                            FunctionId = new WidgetIdentity(vm.CalibrationName, scope, null),
                            ViewModel = vm
                        };
                        var errorWidget = new InclinometrAngularCalibrationErrors()
                        {
                            FunctionId = new WidgetIdentity("Угловые отклонения", scope, null),
                            ViewModel = new AngularCalibrationErrorsVM(vm.Calibrator)
                        };

                        yield return new Calibrator(vm, view, errorWidget);
                    }
                    break;

                case RUSDeviceId.SHOCK_SENSOR:
                    {
                        var vm = new ShockSensorCalibrationVM(uiBusy, device, saveCalibrationFileAsync);
                        var view = new ShockSensorCalibration()
                        {
                            FunctionId = new WidgetIdentity(vm.CalibrationName, scope, null),
                            ViewModel = vm
                        };

                        yield return new Calibrator(vm, view);
                    }
                    break;

                case RUSDeviceId.ROTATIONS_SENSOR:
                    {
                        var vm = new GyroTemperatureCalibrationVM(uiBusy, device, saveCalibrationFileAsync);
                        var view = new GyroTemperatureCalibration()
                        {
                            FunctionId = new WidgetIdentity(vm.CalibrationName, scope, null),
                            ViewModel = vm
                        };

                        yield return new Calibrator(vm, view);
                    }
                    break;
            }
        }
    }
}
