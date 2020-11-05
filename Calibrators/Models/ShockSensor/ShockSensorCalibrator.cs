using Common;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using SpecificMarshallers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyConfig;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;

namespace Calibrators.Models
{
    class ShockSensorCalibrator : CalibratorBase<ShockTestMode, MeasureResultBase, ShockSensorMnemonicType>
    {
        static readonly ConfigAccessor CONFIG2 = Configurable
            .CreateConfig("ShockSensorCalibrator")
            .AddMarshaller<TryParseMarshaller<Interval>>();
        static readonly ConfigProxy<int> NUM_OF_EXTREMA = CONFIG2.Read(10);
        static readonly ConfigProxy<double> PULSE_DURATION_TO_PEAK_WIDTH_COEFFICIENT = CONFIG2.Read(1D);
        static readonly ConfigProxy<double> MIN_PULSE_PERIOD = CONFIG2.Read(1500D);
        static readonly ConfigProxy<Interval> G0_INTERVAL = CONFIG2.Read(new Interval(480, 550));
        static readonly Interval G50_THRESHOLD_INTERVAL = new Interval(35, 45);
        static readonly Interval G100_THRESHOLD_INTERVAL = new Interval(70, 90);
        static readonly Interval G200_THRESHOLD_INTERVAL = new Interval(150, 170);
        static readonly Interval G300_THRESHOLD_INTERVAL = new Interval(230, 260);
        
        public class AxisTestResults : MeasureResultBase, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public double? X0 { get; set; }
            public double? Y0 { get; set; }
            public double? Z0 { get; set; }

            public bool IsFinished => new[] { X0, Y0, Z0 }.All(v => v != null);

            public override Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync()
            {
                throw new NotImplementedException();
            }

            public void Discard()
            {
                X0 = null;
                Y0 = null;
                Z0 = null;
            }
        }

        public class AxisShockTestResults : MeasureResultBase, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public double? XMaxRaw { get; set; }
            public double? YMaxRaw { get; set; }
            public double? ZMaxRaw { get; set; }
            public int PulseDurationCode { get; set; } = 2;

            public bool IsFinished => new[] { XMaxRaw, YMaxRaw, ZMaxRaw }.All(v => v != null);

            public override Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync()
            {
                throw new NotImplementedException();
            }

            public void Discard()
            {
                XMaxRaw = null;
                YMaxRaw = null;
                ZMaxRaw = null;
            }
        }

        readonly AxisTestResults _axisTestResults = new AxisTestResults();
        readonly AxisShockTestResults _axisShockTestResults = new AxisShockTestResults();

        protected override int requestInterval => ((ShockTestMode)CurrentMode).TestType == TestType.AXIS_SHOCK 
            ? 0 
            : 250;
        public override EnhancedObservableCollection<MeasureResultBase> Results { get; } 
            = new EnhancedObservableCollection<MeasureResultBase>();

        public bool IsTestFinished => _axisTestResults.IsFinished;
        public bool IsShockTestFinished => _axisShockTestResults.IsFinished;
        public bool IsTestingFinished => _axisTestResults.IsFinished && _axisShockTestResults.IsFinished;
        public Thresholds CurrentThresholds { get; private set; }
        public override bool CallibrationCanBeGenerated => IsTestingFinished;
        
        IEnumerator<Task<Packet>> _entitiesSource;

        public Interval G50ThresholdInterval { get; } = G50_THRESHOLD_INTERVAL;
        public Interval G100ThresholdInterval { get; } = G100_THRESHOLD_INTERVAL;
        public Interval G200ThresholdInterval { get; } = G200_THRESHOLD_INTERVAL;
        public Interval G300ThresholdInterval { get; } = G300_THRESHOLD_INTERVAL;

        protected override MnemonicInfo<ShockSensorMnemonicType>[] _mnemonics { get; } = new MnemonicInfo<ShockSensorMnemonicType>[]
        {
            new MnemonicInfo<ShockSensorMnemonicType>(ShockSensorMnemonicType.ADXU, "ADXU", x => false, x => x, x => x),
            new MnemonicInfo<ShockSensorMnemonicType>(ShockSensorMnemonicType.ADYU, "ADYU", x => false, x => x, x => x),
            new MnemonicInfo<ShockSensorMnemonicType>(ShockSensorMnemonicType.ADZU, "ADZU", x => false, x => x, x => x),
        };

        public ShockSensorCalibrator(IRUSDevice device)
            : base(device)
        {
            ResetThresholdsToDefault();
        }

        public void ResetThresholdsToDefault()
        {
            CurrentThresholds = new Thresholds()
            {
                G50 = 41,
                G100 = 82,
                G200 = 164,
                G300 = 246,
            };
        }

        protected override async Task handleKnownEntitiesLoopAsync(ShockTestMode mode, IProgress<double> progress, CancellationToken cancellationToken)
        {
            _entitiesSource = receivingStream.GetEnumerator();

            switch (mode.TestType)
            {
                case TestType.AXIS:
                    await testAsync(mode.AxisTestMode, mode.NumOfPoints, progress, cancellationToken);
                    break;
                case TestType.AXIS_SHOCK:
                    await testAsync(mode.AxisShockTestMode, mode.PulseDuration, progress, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException();
            }

            forceRequestFinish();
        }

        async Task testAsync(AxisTestMode mode, int numOfPoints, IProgress<double> progress, CancellationToken cancellation)
        {
            var averagePoints = await aquireAveragePoints();
            switch (mode)
            {
                case AxisTestMode.XZ:
                    _axisTestResults.X0 = checkRangeOrNull(averagePoints.X, G0_INTERVAL);
                    _axisTestResults.Z0 = checkRangeOrNull(averagePoints.Z, G0_INTERVAL);
                    if (_axisTestResults.X0 == null
                        || _axisTestResults.Z0 == null)
                    {
                        Logger.LogError($"Параметры вышли за допустимый диапазон", $"-MSG. Все параметры: {averagePoints}");
                    }
                    break;
                case AxisTestMode.Y:
                    _axisTestResults.Y0 = checkRangeOrNull(averagePoints.Y, G0_INTERVAL);
                    if (_axisTestResults.Y0 == null)
                    {
                        Logger.LogError($"Параметры вышли за допустимый диапазон", $"-MSG. Все параметры: {averagePoints}");
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            return; //////////////////////////////////////////////////////////////////

            async Task<(double X, double Y, double Z)> aquireAveragePoints()
            {
                (double X, double Y, double Z) avg = (0, 0, 0);
                for (int i = 1; i <= numOfPoints; i++)
                {
                    var points = await fetchPoints();
                    avg.X += points.X;
                    avg.Y += points.Y;
                    avg.Z += points.Z;

                    progress.Report(i / (double)numOfPoints);
                }
                avg.X /= numOfPoints;
                avg.Y /= numOfPoints;
                avg.Z /= numOfPoints;

                return avg;
            }
        }

        async Task testAsync(AxisShockTestMode mode, double pulseDuration, IProgress<double> progress, CancellationToken cancellation)
        {
            savePulseDuration();

            var extrema = await aquireExtremaPoint();
            var avg = extrema.Average();
            switch (mode)
            {
                case AxisShockTestMode.X:
                    _axisShockTestResults.XMaxRaw = avg;
                    break;
                case AxisShockTestMode.Y:
                    _axisShockTestResults.YMaxRaw = avg;
                    break;
                case AxisShockTestMode.Z:
                    _axisShockTestResults.ZMaxRaw = avg;
                    break;

                default:
                    throw new NotSupportedException();
            }

            return; //////////////////////////////////////////////////////////////////

            void savePulseDuration()
            {
                if (_axisShockTestResults.PulseDurationCode != pulseDuration)
                {
                    _axisShockTestResults.PulseDurationCode = getDurationCode();
                    _axisShockTestResults.XMaxRaw = null;
                    _axisShockTestResults.YMaxRaw = null;
                    _axisShockTestResults.ZMaxRaw = null;

                    int getDurationCode()
                    {
                        switch (pulseDuration)
                        {
                            case 2D:
                                return 2;
                            case 1D:
                                return 1;
                            case 0.5D:
                                return 0;
                            case 0.25D:
                                return -1;

                            default:
                                throw new NotSupportedException("Некорректная длительность импульса");
                        }
                    }
                }
            }
            async Task<double[]> aquireExtremaPoint()
            {
                double peakDuration = PULSE_DURATION_TO_PEAK_WIDTH_COEFFICIENT * pulseDuration; 

                var probability = 0D; // Probability of all points acquired
                var points = new List<(double DTime, double Point)>();
                while (probability < 1)
                {
                    var point = await fetchPoint();
                    var dTime = _entitiesSource.Current.Result.DTime;
                    points.Add((dTime, point));

                    // Probability that this points are measured on impulse's peak / num of measurments required
                    probability += (peakDuration / dTime) * (dTime / MIN_PULSE_PERIOD) / NUM_OF_EXTREMA;
                    progress.Report(probability);
                    cancellation.ThrowIfCancellationRequested();
                }

                return points
                    .Select(p => p.Point)
                    .Max(NUM_OF_EXTREMA)
                    .ToArray();

                async Task<double> fetchPoint()
                {
                    var measureResult = await fetchPoints();
                    switch (mode)
                    {
                        case AxisShockTestMode.X:
                            return measureResult.X;
                        case AxisShockTestMode.Y:
                            return measureResult.Y;
                        case AxisShockTestMode.Z:
                            return measureResult.Z;

                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }
        double? checkRangeOrNull(double value, Interval range)
        {
            var isOk = checkRange(value, range);

            return isOk ? value : (double?)null;
        }
        bool checkRange(double value, Interval range)
        {
            var isOk = range.Contains(value);
            if (!isOk)
            {
                Logger.LogErrorEverywhere("Значение вне допустимого диапазона");
            }

            return isOk;
        }
        async Task loadDataPacketConfiguration()
        {
            var result = await _device.ReadAsync(Command.DATA_PACKET_CONFIGURATION_FILE, DeviceOperationScope.DEFAULT, CancellationToken.None);
            if (result.Status != ReadStatus.OK)
            {
                var msg = "Не удалось получить формат пакета данных";
                Logger.LogErrorEverywhere(msg);

                throw new InvalidOperationException(msg);
            }
        }
        async Task<V3> fetchPoints()
        {
            var values = await fetchValues();
            return new V3(
                values.Single(v => v.Type == ShockSensorMnemonicType.ADXU).Value,
                values.Single(v => v.Type == ShockSensorMnemonicType.ADYU).Value,
                values.Single(v => v.Type == ShockSensorMnemonicType.ADZU).Value);
        }
        async Task<(ShockSensorMnemonicType Type, double Value)[]> fetchValues()
        {
            var raw = await _entitiesSource.GetNextOrThrow();
            var points = new(ShockSensorMnemonicType Type, double Value)[_mnemonics.Length];
            foreach (var i in _mnemonics.Length.Range())
            {
                var mInfo = _mnemonics[i];
                var value = raw.Entities.Find(e => e.Info.Type == mInfo.Type).Value.Value;

                points[i] = (mInfo.Type, value);
            }

            return points;
        }

        public void RecalculateThresholds()
        {
            if (IsTestingFinished)
            {
                CurrentThresholds.G100 =
                    (_axisShockTestResults.XMaxRaw - _axisTestResults.X0 +
                    _axisShockTestResults.YMaxRaw - _axisTestResults.Y0 +
                    _axisShockTestResults.ZMaxRaw - _axisTestResults.Z0).Value / 3;
                CurrentThresholds.G50 = CurrentThresholds.G100 / 2;
                CurrentThresholds.G200 = CurrentThresholds.G100 * 2;
                CurrentThresholds.G300 = CurrentThresholds.G100 * 3;
            }
            else
            {
                throw new InvalidOperationException("Необходимые замеры не были произведены");
            }
        }

#warning file should not be assembled here! What if format changes?
        public override async Task<IEnumerable<CalibrationFileEntity>> GenerateCalibrationCoefficientsAsync()
        {
            if (IsTestFinished)
            {
                if (!checkRange(CurrentThresholds.G50, G50_THRESHOLD_INTERVAL) ||
                    !checkRange(CurrentThresholds.G100, G100_THRESHOLD_INTERVAL) ||
                    !checkRange(CurrentThresholds.G200, G200_THRESHOLD_INTERVAL) ||
                    !checkRange(CurrentThresholds.G300, G300_THRESHOLD_INTERVAL))
                {
                    throw new InvalidOperationException();
                }

                var entities = new Enumerable<CalibrationFileEntity>
                {
                    CalibrationFileEntity.CreateConstantsTable(
                    "G0_",
                    DataTypes.UINT16,
                    new[]
                    {
                        _axisTestResults.X0,
                        _axisTestResults.Y0,
                        _axisTestResults.Z0,
                    }.Select(v => v.Value.ToUInt16()).ToArray()),

                    CalibrationFileEntity.CreateConstantsTable(
                    "POR_",
                    DataTypes.UINT16,
                    new[]
                    {
                        CurrentThresholds.G50,
                        CurrentThresholds.G100,
                        CurrentThresholds.G200,
                        CurrentThresholds.G300,
                    }.Select(v => v.ToUInt16()).ToArray()),

                    CalibrationFileEntity.CreateConstantsTable(
                    "TMR",
                    DataTypes.UINT16,
                    new[]
                    {
                        _axisShockTestResults.PulseDurationCode
                    }.Select(v => v.ToUInt16()).ToArray()),
                };

                return await Task.FromResult(entities);
            }
            else
            {
                throw new InvalidOperationException("Необходимые замеры не были произведены");
            }
        }

        public void DiscardShockCalibration()
        {
            _axisShockTestResults.Discard();
        }

        protected override void discardCalibration()
        {
            Results.Clear();
            _axisTestResults.Discard();
            _axisShockTestResults.Discard();
            _entitiesSource = null;
        }
    }
}
