using Common;
using Calibrators.Models;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Vectors;
using WPFUtilities.Types;

namespace Calibrators.ViewModels
{
    internal class GyroTemperatureCalibrationVM 
        : CalibrationBaseVM<GyroTemperatureCalibrator, GyroTemperatureCalibrator.Modes>
    {
        const GyroTemperatureCalibrator.Modes CALIBRATION_NOT_STARTED_MODE = GyroTemperatureCalibrator.Modes.STAGE_1 - 2;
        const GyroTemperatureCalibrator.Modes WAITING_FOR_STAGE1_TO_START = GyroTemperatureCalibrator.Modes.STAGE_1 - 1;

        protected override bool canBegin => lastFinishedMode == CALIBRATION_NOT_STARTED_MODE;
        protected override bool canDiscard => lastFinishedMode != CALIBRATION_NOT_STARTED_MODE;

        public RotationSpeedVM RotationSpeed { get; }
        public DoubleValueVM AveragePoints { get; }

        public GyroTemperatureCalibrationVM(
            BusyObject busy,
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
            : base(busy, device, saveCalibrationFileAsync)
        {
            AveragePoints = new DoubleValueVM(new Interval(10, 1000), true, 0,
                () => _calibrator.AveragePoints,
                v => _calibrator.AveragePoints = v.Round(),
                _calibrator);
            AveragePoints.ModelValue = 20;
            RotationSpeed = new RotationSpeedVM(_calibrator);
        }
        protected override void onBegin()
        {
            lastFinishedMode = WAITING_FOR_STAGE1_TO_START;
        }
        protected override void onDiscard()
        {
            lastFinishedMode = CALIBRATION_NOT_STARTED_MODE;
        }

        public override string CalibrationName => "Термокалибровка гироскопа";
        public override IRichProgress MeasureProgress => new ProgressVM();

        protected override bool canExport => false;

        public override EnhancedObservableCollection<GyroTemperatureCalibrator.Modes> Modes { get; }
            = new EnhancedObservableCollection<GyroTemperatureCalibrator.Modes>(EnumUtils.GetValues<GyroTemperatureCalibrator.Modes>());

        protected override MeasureResultBase exportingResult => throw new NotSupportedException();

        protected override FileType calibrationFileType => FileType.TEMPERATURE_CALIBRATION;

        protected override GyroTemperatureCalibrator instantiateCalibrator()
        {
            return new GyroTemperatureCalibrator(_device);
        }

        protected override Task loadResultAsync(string fileName, byte[] fileData)
        {
            throw new NotSupportedException();
        }

        GyroTemperatureCalibrator.Modes lastFinishedMode { get; set; } = CALIBRATION_NOT_STARTED_MODE;

        protected override void onMeasureFinished()
        {
            if (_calibrator.State == MeasureState.FINISHED_SUCCESSFULLY)
            {
                lastFinishedMode = SelectedMode;
            }
        }
        protected override bool canStartMeasure(CommandParameter<GyroTemperatureCalibrator.Modes> mode)
        {
            return mode.Value - 1 <= lastFinishedMode;
        }
        protected override bool canFinishMeasure(CommandParameter<GyroTemperatureCalibrator.Modes> mode)
        {
            return mode.Value == SelectedMode;
        }

        protected override void updateModelBindings()
        {
            Status = _calibrator.State.ToString(
                s => s.ToString(),
                (MeasureState.WAITING_FOR_START, "ожидание запуска"),
                (MeasureState.MEASURING, "замер"),
                (MeasureState.WAITING_FOR_FINISH, "ожидание завершения"),
                (MeasureState.WAITING_FOR_CALIBRATION_MODE_SET, "установка режима"),
                (MeasureState.FINISHED_SUCCESSFULLY, "замер успешно завершен"),
                (MeasureState.FINISHED_WITH_ERROR, "ошибка во время замера"),
                (MeasureState.CANCELLED, "замер прерван"));
        }
    }
}
