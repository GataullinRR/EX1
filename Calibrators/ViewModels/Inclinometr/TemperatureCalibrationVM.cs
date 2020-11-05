using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using Calibrators.Models;
using MVVMUtilities.Types;
using System.Threading;
using System.IO;
using static Calibrators.Models.InclinometrTemperatureCalibrator;
using DeviceBase;
using DeviceBase.IOModels;
using DeviceBase.Models;
using DeviceBase.Devices;
using System.Windows;
using Calibrators.ViewModels.Inclinometr;
using Common;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class TemperatureCalibrationVM
        : CalibrationBaseVM<InclinometrTemperatureCalibrator, IncTCalAngle>
    {
        public override string CalibrationName => "Температурная калибровка";
        protected override FileType calibrationFileType => FileType.TEMPERATURE_CALIBRATION;
        protected override bool canExport => _calibrator.Results.Any(r => r.Mode == SelectedCalibrationMode);
        public override IncTCalAngle SelectedMode
        {
            get => SelectedCalibrationMode;
            protected set => SelectedCalibrationMode = value;
        }
        public override IRichProgress MeasureProgress { get; } = new ProgressVM();
        protected override MeasureResultBase exportingResult => _calibrator.Results.Single(r => r.Mode == SelectedCalibrationMode);

        public InclinometrTemperatureCalibrator Calibrator => _calibrator;
        public override EnhancedObservableCollection<IncTCalAngle> Modes { get; } = new EnhancedObservableCollection<IncTCalAngle>(
            Enum.GetValues(typeof(IncTCalAngle))
            .ToEnumerable()
            .Select(v => (IncTCalAngle)v));
        public IncTCalAngle SelectedCalibrationMode { get; set; } = IncTCalAngle.INC0_AZI0_GTF0;

        public TemperatureCalibrationVM(
            BusyObject busy,
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
            : base(busy, device, saveCalibrationFileAsync)
        {

        }

        protected override InclinometrTemperatureCalibrator instantiateCalibrator()
        {
            return new InclinometrTemperatureCalibrator(_device);
        }

        protected override async Task loadResultAsync(string fileName, byte[] fileData)
        {
            var result = await MeasureResult.DeserializeAsync(fileName, fileData);
            _calibrator.Results.Remove(r => r.Mode == result.Mode);
            _calibrator.Results.Add(result);
        }

        protected override void updateModelBindings()
        {
            Status = _calibrator.CalibrationState.ToString(
                s => s.ToString(),
                (States.WAITING_FOR_START, "ожидание запуска"),
                (States.WAITING_FOR_HEATING_UP, $"нагревание до {_calibrator.TemperatureRange.To} °C"),
                (States.WAITING_FOR_COOLING_DOWN, $"остывание до {_calibrator.TemperatureRange.From} °C"),
                (States.WAITING_FOR_FINISH, "ожидание завершения"),
                (States.WAITING_FOR_CALIBRATION_MODE_SET, "установка режима"),
                (States.FINISHED_SUCCESSFULLY, "замер успешно завершен"),
                (States.FINISHED_WITH_ERROR, "ошибка во время замера"),
                (States.CANCELLED, "замер прерван"));
        }
    }
}
