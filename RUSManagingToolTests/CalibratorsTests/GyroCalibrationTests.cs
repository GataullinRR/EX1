using NUnit.Framework;
using Calibrators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceBase.Devices;
using MVVMUtilities.Types;
using Calibrators.Views;
using RUSManagingTool.ViewModels;
using Calibrators.Models;
using System.Threading;
using System.Windows.Threading;

namespace Calibrators.Tests
{
    [TestFixture()]
    public class CalibratorsFactory_Tests
    {
        [Test(), Apartment(ApartmentState.STA)]
        public async Task GyroCalibrationProcess()
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

            const byte ROTATION_SENSOR_ID = 0b00000111;

            var mainVM = new MainVM();
            var rotationSensorVM = mainVM.DevicesVM.SelectedDevice = mainVM.DevicesVM.SupportedDevices.Single(d => d.ToString().Contains(ROTATION_SENSOR_ID.ToString()));
            var calibratorVM = ((GyroTemperatureCalibration)(rotationSensorVM.Calibrators.Single(c => c.CalibrationName == "Термокалибровка гироскопа").View)).ViewModel;

            calibratorVM.AveragePoints.ModelValue = 20;

            calibratorVM.StartMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_1);
            await calibratorVM.IsBusy.WaitAsync();

            calibratorVM.StartMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_2_OFFSET_CALC);
            await Task.Delay(1000);
            calibratorVM.FinishMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_2_OFFSET_CALC);
            await calibratorVM.IsBusy.WaitAsync();

            calibratorVM.RotationSpeed.IsByNominal = true;
            calibratorVM.RotationSpeed.Speed.ModelValue = 50;

            calibratorVM.StartMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_3);
            await Task.Delay(1000);
            calibratorVM.FinishMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_3);
            await calibratorVM.IsBusy.WaitAsync();

            calibratorVM.StartMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_4);
            await Task.Delay(1000);
            calibratorVM.FinishMeasure.ExecuteIfCanOrThrow(GyroTemperatureCalibrator.Modes.STAGE_4);
            await calibratorVM.IsBusy.WaitAsync();

            calibratorVM.SaveCallibrationFile.ExecuteIfCanOrThrow();
        }

        public static void RunInWpfSyncContext(Func<Task> function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            var prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new DispatcherSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                var task = function();
                if (task == null)
                {
                    throw new InvalidOperationException();
                }

                var frame = new DispatcherFrame();
                var t2 = task.ContinueWith(x => { frame.Continue = false; }, TaskScheduler.Default);
                Dispatcher.PushFrame(frame);   // execute all tasks until frame.Continue == false

                task.GetAwaiter().GetResult(); // rethrow exception when task has failed 
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }
    }
}