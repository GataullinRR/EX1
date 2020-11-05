using Calibrators.Models;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.Models;
using MVVMUtilities;
using MVVMUtilities.Types;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using WPFUtilities.Types;

namespace Calibrators.ViewModels
{
    internal class ShockSensorCalibrationVM : CalibrationBaseVM<ShockSensorCalibrator, object>, INotifyPropertyChanged
    {
        public ShockSensorCalibrator Calibrator => _calibrator;
        public int NumOfTestingPoints { get; set; } = 20;
        public PulseDurationVM PulseDurationVM { get; }

        public ActionCommand SetDefaultThresholds { get; }
        public ActionCommand CalculateThresholds { get; }

        public override string CalibrationName => "Калибровка";
        public override IRichProgress MeasureProgress { get; } = new ProgressVM();
        protected override bool canExport => _calibrator.CallibrationCanBeGenerated;
        protected override MeasureResultBase exportingResult => throw new NotSupportedException();
        protected override FileType calibrationFileType => FileType.CALIBRATION;

        public ShockSensorCalibrationVM(
            BusyObject busy,
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
            : base(busy, device, saveCalibrationFileAsync)
        {
            PulseDurationVM = new PulseDurationVM(IsBusy, _calibrator);

            SetDefaultThresholds = new ActionCommand(
                (Action)Calibrator.ResetThresholdsToDefault,
                IsBusy
            );
            CalculateThresholds = new ActionCommand(
                (Action)Calibrator.RecalculateThresholds,
                () => Calibrator.IsTestingFinished && !IsBusy,
                IsBusy, Calibrator
            );
        }

        protected override ShockSensorCalibrator instantiateCalibrator()
        {
            return new ShockSensorCalibrator(_device);
        }

        protected override object getModelsTestMode(object mode)
        {
            return instantiateTestMode((dynamic)mode);
        }
        ShockTestMode instantiateTestMode(AxisTestMode testMode)
        {
            return new ShockTestMode(testMode, NumOfTestingPoints);
        }
        ShockTestMode instantiateTestMode(AxisShockTestMode testMode)
        {
            return new ShockTestMode(testMode, PulseDurationVM.SelectedPulseDuration);
        }

        protected override bool canStartMeasure(CommandParameter<object> mode)
        {
            if (mode.Value is AxisTestMode atm)
            {
                switch (atm)
                {
                    case AxisTestMode.XZ:
                    case AxisTestMode.Y:
                        return true;

                    default:
                        throw new NotSupportedException();
                }
            }
            else if (mode.Value is AxisShockTestMode astm)
            {
                switch (astm)
                {
                    case AxisShockTestMode.X:
                    case AxisShockTestMode.Y:
                    case AxisShockTestMode.Z:
                        return Calibrator.IsTestFinished;

                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected override Task loadResultAsync(string fileName, byte[] fileData)
        {
            throw new NotImplementedException();
        }
    }
}
