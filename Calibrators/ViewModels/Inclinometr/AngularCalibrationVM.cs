using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using MVVMUtilities.Types;
using Calibrators.Models;
using System.Threading;
using System.Windows;
using System.IO;
using DeviceBase;
using DeviceBase.IOModels;
using DeviceBase.Models;
using DeviceBase.Devices;
using Vectors;
using System.ComponentModel;
using static Calibrators.Models.InclinometrAngularCalibrator;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class AngularCalibrationVM : CalibrationBaseVM<InclinometrAngularCalibrator, PrecisePosition>
    {
        public PositionMeasureInfoVM[] CalibrationPositions { get; } = EnumUtils
            .GetValues<Position>()
            .Select(p => new PositionMeasureInfoVM(PrecisePosition.FromOffsets(p, 0, 0, 0))).ToArray();
        public PositionMeasureInfoVM SelectedCalibrationPosition { get; set; }

        public InclinometrAngularCalibrator Calibrator => _calibrator;
        public PrecisePosition ActualPosition => _calibrator.ActualPosition;
        public ProgressVM TotalProgress { get; } = new ProgressVM();
        public DoubleValueVM BTotal { get; } 
        public DoubleValueVM DipAngle { get; } 
        public IEnumerable<DeviceDataVM> DeviceData { get; }

        public bool GoToNextWhenFinished { get; set; } = true;

        public override string CalibrationName => "Угловая калибровка";
        public override IRichProgress MeasureProgress { get; } = new ProgressVM();
        protected override FileType calibrationFileType => FileType.CALIBRATION;
        public override PrecisePosition SelectedMode
        {
            get => SelectedCalibrationPosition.Position;
            protected set => SelectedCalibrationPosition = CalibrationPositions.Find(p => p.Position == value).Value;
        }
        public override EnhancedObservableCollection<PrecisePosition> Modes { get; }
        protected override bool canExport => _calibrator.Results.Any();
        protected override MeasureResultBase exportingResult => _calibrator.Results.Single();

        public AngularCalibrationVM(
            BusyObject busy,
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
            : base(busy, device, saveCalibrationFileAsync)
        {
            _preferences.RequestCancellationAcknowledgement = false;

            BTotal = new DoubleValueVM(new Interval(0, 2), true, 8,
                () => _calibrator.Constants.BTotal,
                v => _calibrator.Constants.BTotal = v, _calibrator);
            DipAngle = new DoubleValueVM(new Interval(-90, 90), true, 8,
                () => _calibrator.Constants.DipAngle,
                v => _calibrator.Constants.DipAngle = v, _calibrator);
            BTotal.ModelValue = _calibrator.Constants.BTotal;
            DipAngle.ModelValue = _calibrator.Constants.DipAngle;

            Modes = new EnhancedObservableCollection<PrecisePosition>(CalibrationPositions.Select(p => p.Position));
            SelectedCalibrationPosition = CalibrationPositions.FirstElement();
            DeviceData = new DeviceDataVM[]
            {
                new DeviceDataVM(
                    "INC",
                    () => _calibrator.ActualPosition?.Inc,
                    () => SelectedCalibrationPosition.Position.Inc,
                    v  => SelectedCalibrationPosition.Position.IncOffset += v - SelectedCalibrationPosition.Position.Inc,
                    () => (new Interval(-5, 5) + SelectedCalibrationPosition.Position.Inc - SelectedCalibrationPosition.Position.IncOffset).NegativeToZero,
                    new NotificationAggregator(_calibrator, this)),
                new DeviceDataVM(
                    "AZI",
                    () => _calibrator.ActualPosition?.Azi,
                    () => SelectedCalibrationPosition.Position.Azi,
                    v  => SelectedCalibrationPosition.Position.AziOffset += v - SelectedCalibrationPosition.Position.Azi,
                    () => (new Interval(-5, 5) + SelectedCalibrationPosition.Position.Azi).NegativeToZero,
                    new NotificationAggregator(_calibrator, this)),
                new DeviceDataVM(
                    "GTF",
                    () => _calibrator.ActualPosition?.Gtf,
                    () => SelectedCalibrationPosition.Position.Gtf,
                    v  => SelectedCalibrationPosition.Position.GtfOffset += v - SelectedCalibrationPosition.Position.Gtf,
                    () => (new Interval(-5, 5) + SelectedCalibrationPosition.Position.Gtf).NegativeToZero,
                    new NotificationAggregator(_calibrator, this)),
            };
        }

        protected override InclinometrAngularCalibrator instantiateCalibrator()
        {
            return new InclinometrAngularCalibrator(_device, TotalProgress);
        }

        protected override void onMeasureFinished()
        {
            if (_calibrator.State == MeasureState.FINISHED_SUCCESSFULLY &&
                GoToNextWhenFinished)
            {
                SelectedCalibrationPosition = CalibrationPositions
                    .SkipWhile(p => p != SelectedCalibrationPosition)
                    .Skip(1)
                    .Take(1)
                    .SingleOrDefault()
                    ?? SelectedCalibrationPosition;
            }
        }

        protected override async Task loadResultAsync(string fileName, byte[] fileData)
        {
            var result = await MeasureResult.DeserializeAsync(fileName, fileData);
            _calibrator.Results.Clear();
            _calibrator.Results.Add(result);
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
            foreach (var i in CalibrationPositions.Length.Range())
            {
                var info = CalibrationPositions[i];
                info.IsMeasured = _calibrator.IsPositionMeasured(info.Position.Position);
            }
        }
    }
}