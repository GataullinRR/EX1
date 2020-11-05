using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.Models;
using MVVMUtilities.Types;
using TinyConfig;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;

namespace Calibrators.Models
{
    internal class GyroTemperatureCalibrator : CalibratorBase<
            GyroTemperatureCalibrator.Modes,
            GyroTemperatureCalibrator.MeasureResult,
            GyroTemperatureCalibrator.MnemonicTypes>
    {
        static readonly Interval T_RANGE = new Interval(0, 155);

        public enum MnemonicTypes
        {
            GYRO_TEMPERATURE,
            HALL_TEMPERATURE,
            ADSG,
            ADH1,
            ADH2,
            HALL_VELOCITY,
            GYRO_VELOCITY
        }

        public enum Modes
        {
            STAGE_1 = 1,
            STAGE_2_OFFSET_CALC = 2,
            STAGE_3 = 3,
            STAGE_4 = 4
        }

        public class MeasureResult : MeasureResultBase
        {
            public IEnumerable<double> ADSGAvg { get; set; }
            public IEnumerable<double> ADH1Avg { get; set; }
            public IEnumerable<double> ADH2Avg { get; set; }
            public IEnumerable<double> RotationVelocity { get; set; }
            public IEnumerable<double> ADSGAvgRot { get; set; }

            public MeasureResult()
            {

            }

            public override Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync()
            {
                throw new NotImplementedException();
            }

            public bool IsAllSet()
            {
                return new[] { ADSGAvg, ADH1Avg, ADH2Avg, ADSGAvgRot, RotationVelocity }.All(v => v != null);
            }

            public void Discard()
            {
                ADSGAvg = null;
                ADH1Avg = null;
                ADH2Avg = null;
                RotationVelocity = null;
                ADSGAvgRot = null;
            }
        }

        readonly MeasureResult _measureResult = new MeasureResult();
        public override EnhancedObservableCollection<MeasureResult> Results { get; }
            = new EnhancedObservableCollection<MeasureResult>();

        public override bool CallibrationCanBeGenerated => _measureResult.IsAllSet();

        public int AveragePoints { get; set; } = 20;
        protected override int requestInterval => 200;

        public double ActualRotationSpeed { get; private set; } = 0;
        public bool IsRotationSpeedSetByUser { get; set; }
        public void SetRotationSpeed(double speed)
        {
            if (ActualRotationSpeed != speed)
            {
                if (IsRotationSpeedSetByUser)
                {
                    ActualRotationSpeed = speed;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        protected override MnemonicInfo<MnemonicTypes>[] _mnemonics { get; } = new MnemonicInfo<MnemonicTypes>[]
        {
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.ADH1, "ADH1", x => (ushort)x == ushort.MaxValue || x < 0, x => x, x => x),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.ADH2, "ADH2", x => (ushort)x == ushort.MaxValue || x < 0, x => x, x => x),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.ADSG, "ADSG", x => (ushort)x == ushort.MaxValue || x < 0, x => x, x => x),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.GYRO_TEMPERATURE, "TEM1", x => (ushort)x == ushort.MaxValue, x => x, x => x / 10),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.HALL_TEMPERATURE, "TEM2", x => (ushort)x == ushort.MaxValue, x => x, x => x / 10),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.HALL_VELOCITY, "SRO2", x => (ushort)x == ushort.MaxValue, x => x, x => x.Abs() / 100),
            new MnemonicInfo<MnemonicTypes>(MnemonicTypes.GYRO_VELOCITY, "SGY2", x => (ushort)x == ushort.MaxValue, x => x, x => x.Abs() / 100),
        };

        IEnumerable<IDataEntity> _factoryFileHeader;
        /// <summary>
        /// All packets from the time when measure was started
        /// </summary>
        readonly List<IEnumerable<KnownEntity>> _allPackets = new List<IEnumerable<KnownEntity>>();
        DisplaceCollection<IEnumerable<KnownEntity>> _averagingCollection;

        public GyroTemperatureCalibrator(IRUSDevice device) : base(device)
        {
            Results.Add(_measureResult);
        }
        protected override IEnumerable<ViewDataEntity> transformAquiredData(
            IEnumerable<(IDataEntity Entity, KnownEntity KnownEntity)> entitiesReceived)
        {
            var newEntities = Enumerable.Empty<ViewDataEntity>();
            if (CurrentMode.Equals(Modes.STAGE_4))
            {
                entitiesReceived = entitiesReceived.MakeCached();
                var knownOnly = entitiesReceived.Select(e => e.KnownEntity).SkipNulls();
                var vGyro = knownOnly.Find(e => e.Info.Type == MnemonicTypes.GYRO_VELOCITY).Value;
                var vHall = knownOnly.Find(e => e.Info.Type == MnemonicTypes.HALL_VELOCITY).Value;
                var absError = vGyro.RawValue - vHall.RawValue;
#warning magic number
                var relativeError = absError / vHall.RawValue * 100;

                newEntities = new Enumerable<ViewDataEntity>
                {
                    new ViewDataEntity("VgAbsErr", absError, false),
                    new ViewDataEntity("VgRelErr", relativeError, false),
                };
            }

            return Enumerable.Concat(base.transformAquiredData(entitiesReceived), newEntities);
        }

        protected override async Task beginCalibrationAsync()
        {
            var result = await _device.ReadAsync(Command.FACTORY_SETTINGS_FILE, DeviceOperationScope.DEFAULT, CancellationToken.None);

            if (result.Status == ReadStatus.OK)
            {
                Results.Clear();
                _factoryFileHeader = Files.ExtractHeader(result.Entities).MakeCached();
                _averagingCollection = new DisplaceCollection<IEnumerable<KnownEntity>>(AveragePoints);
                _allPackets.Clear();

                var serial = Files.GetFileEntity(_factoryFileHeader, FileEntityType.SERIAL_NUMBER).Value;
                Logger.LogInfoEverywhere($"Начата калибровка устройства \"{_device.Name}\" SN:{serial}");
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        protected override async Task onMeasureAsync(Modes mode)
        {
            Logger.LogInfoEverywhere($"Замер {mode} запущен");

            _allPackets.Clear();

            if (mode == Modes.STAGE_1)
            {
                var calBurnResult = await _device.BurnAsync(Command.CALIBRATION_FILE, getDefaultCalFile(), DeviceOperationScope.DEFAULT, CancellationToken.None);

                if (calBurnResult.Status != BurnStatus.OK)
                {
                    throw new InvalidOperationException();
                }
            }
            else if (mode == Modes.STAGE_2_OFFSET_CALC)
            {
                _measureResult.Discard();
            }
            else if (mode == Modes.STAGE_3)
            {
                _measureResult.ADSGAvgRot = null;
            }
            else if (mode != Modes.STAGE_4)
            {
                throw new NotSupportedException();
            }

            IEnumerable<IDataEntity> getDefaultCalFile()
            {
                var file = Files
                    .Descriptors[new FileDescriptorsTarget(FileType.CALIBRATION, "01", _device.Id)]
                    .Descriptors
                    .Select(d => d.FileDefaultDataEntity);

                return setDateAndHeader(file);
            }
        }

        protected override async Task handleDataPacketAsync(Modes mode, 
            IProgress<double> progress, 
            IEnumerable<KnownEntity> entities)
        {
            _allPackets.Add(entities);

            switch (mode)
            {
                case Modes.STAGE_1:
                    _averagingCollection.Add(entities);
                    if (_averagingCollection.Count == _averagingCollection.Capacity)
                    {
                        var dT = calculateCoefficient();
                        var file = await assembleFileAsync();
                        var result = await _device.BurnAsync(Command.CALIBRATION_FILE, file, DeviceOperationScope.DEFAULT, CancellationToken.None);
                        if (result.Status != BurnStatus.OK)
                        {
                            Logger.LogErrorEverywhere("Не удалось записать файл калибровки");

                            throw new InvalidOperationException();
                        }
                        forceRequestFinish();

                        double calculateCoefficient()
                        {
                            var t1Index = entities.Find(e => e.Info.Type == MnemonicTypes.GYRO_TEMPERATURE).Index;
                            var t2Index = entities.Find(e => e.Info.Type == MnemonicTypes.HALL_TEMPERATURE).Index;
                            var avg = _averagingCollection
                                .Select(kes => kes.Select(ke => ke.Value))
                                .Aggregate((acc, cur) => acc.Sum(cur))
                                .DivEach(_averagingCollection.Count)
                                .ToArray();

#warning magic number!
                            return (avg[t2Index] - avg[t1Index]) * 10;
                        }
                        async Task<IEnumerable<IDataEntity>> assembleFileAsync()
                        {
                            var coefficients = CalibrationFileEntity
                                .CreateConstantsTable("dTe", DataTypes.INT16, new[] { dT.ToInt16() })
                                .ToSequence()
                                .ToArray();
                            var f = await new CalibrationFileGenerator(_device, FileType.CALIBRATION, "01")
                                .GenerateAsync(coefficients);
                            f = setDateAndHeader(f);

                            return f;
                        }
                    }
                    break;

                case Modes.STAGE_2_OFFSET_CALC:
                    setWaitingForFinish();
                    break;

                case Modes.STAGE_3:
                    setWaitingForFinish();
                    updateActualRotationSpeedIfRequired();
                    break;

                case Modes.STAGE_4:
                    setWaitingForFinish();
                    updateActualRotationSpeedIfRequired();
                    break;

                default:
                    throw new NotSupportedException();
            }

            void updateActualRotationSpeedIfRequired()
            {
                if (CurrentMode.IsOneOf(Modes.STAGE_3, Modes.STAGE_4) && 
                    !IsRotationSpeedSetByUser)
                {
                    ActualRotationSpeed = entities.Single(e => e.Info.Type == MnemonicTypes.HALL_VELOCITY).Value;
                }
            }
        }
        IEnumerable<IDataEntity> setDateAndHeader(IEnumerable<IDataEntity> file)
        {
            file = Files.MergeHeader(file, _factoryFileHeader);
            file = Files.SetBurnDate(file, DateTime.UtcNow);

            return file;
        }
        protected override Task finishAsync(Modes mode, DateTime measureStart)
        {
            switch (mode)
            {
                case Modes.STAGE_1:
                    break;
                case Modes.STAGE_2_OFFSET_CALC:
                    {
                        var avg = calculateAverage();

                        _measureResult.ADSGAvg = avg.ADSG;
                        _measureResult.ADH1Avg = avg.ADH1;
                        _measureResult.ADH2Avg = avg.ADH2;
                    }
                    break;

                case Modes.STAGE_3:
                    {
                        var avg = calculateAverage();

                        _measureResult.ADSGAvgRot = avg.ADSG;
                        _measureResult.RotationVelocity = IsRotationSpeedSetByUser
                            ? Enumerable.Repeat(ActualRotationSpeed, T_RANGE.IntLen + 1).ToArray()
                            : avg.RotVel;
                    }
                    break;

                case Modes.STAGE_4:
                    break;

                default:
                    throw new NotSupportedException();
            }

            return Task.FromResult(true);

            (double[] ADSG, double[] ADH1, double[] ADH2, double[] RotVel) calculateAverage()
            {
                var t1 = getCurve(MnemonicTypes.GYRO_TEMPERATURE);
                var t2 = getCurve(MnemonicTypes.HALL_TEMPERATURE);
                var adh1 = getCurve(MnemonicTypes.ADH1);
                var adh2 = getCurve(MnemonicTypes.ADH2);
                var adsg = getCurve(MnemonicTypes.ADSG);
                var rotVel = getCurve(MnemonicTypes.HALL_VELOCITY);

                var avgADH1 = new double[T_RANGE.IntLen + 1];
                var avgADH2 = new double[T_RANGE.IntLen + 1];
                var avgADSG = new double[T_RANGE.IntLen + 1];
                var avgRotVel = new double[T_RANGE.IntLen + 1];
                for (int t = T_RANGE.IntFrom, i = 0; t <= T_RANGE.IntTo; t++, i++)
                {
                    var closestT1i = t1.FindClosestPoint(t).Index;
                    var closestT2i = t2.FindClosestPoint(t).Index;
                    if ((t1[closestT1i] - t).Abs() < 0.5) // Point found
                    {
                        avgADSG[i] = adsg.GetRangeAroundSafe(closestT1i, AveragePoints).Average();
                    }
                    else
                    {
                        avgADSG[i] = avgADSG[(i - 1).NegativeToZero()];
                    }
                    if ((t2[closestT2i] - t).Abs() < 0.5) // Point found
                    {
                        avgADH1[i] = adh1.GetRangeAroundSafe(closestT2i, AveragePoints).Average();
                        avgADH2[i] = adh2.GetRangeAroundSafe(closestT2i, AveragePoints).Average();
                        avgRotVel[i] = rotVel.GetRangeAroundSafe(closestT2i, AveragePoints).Average();
                    }
                    else
                    {
                        avgADH1[i] = avgADH1[(i - 1).NegativeToZero()];
                        avgADH2[i] = avgADH2[(i - 1).NegativeToZero()];
                        avgRotVel[i] = avgRotVel[(i - 1).NegativeToZero()];
                    }
                }
                var firstAvgADSG = avgADSG.FirstOrDefault(v => v != 0);
                var firstAvgADH1 = avgADH1.FirstOrDefault(v => v != 0);
                var firstAvgADH2 = avgADH2.FirstOrDefault(v => v != 0);
                var firstAvgRotVel = avgRotVel.FirstOrDefault(v => v != 0);

                return (
                    avgADSG.Select(v => v == 0 ? firstAvgADSG : v).ToArray(),
                    avgADH1.Select(v => v == 0 ? firstAvgADH1 : v).ToArray(), 
                    avgADH2.Select(v => v == 0 ? firstAvgADH2 : v).ToArray(),
                    avgRotVel.Select(v => v == 0 ? firstAvgRotVel : v).ToArray());
            }
            double[] getCurve(MnemonicTypes mnemonic)
            {
                return _allPackets
                    .Select(row => row.Single(e => e.Info.Type == mnemonic).Value)
                    .ToArray();
            }
        }

        public override async Task<IEnumerable<CalibrationFileEntity>> GenerateCalibrationCoefficientsAsync()
        {
            var kGyro = _measureResult.RotationVelocity
                .Div(_measureResult.ADSGAvgRot.Sub(_measureResult.ADSGAvg))
                .AbsEach();

            Logger.LogInfo(null, "RotVel = " + _measureResult.RotationVelocity.AsString(" "));
            Logger.LogInfo(null, "ADSGAvg = " + _measureResult.ADSGAvg.AsString(" "));
            Logger.LogInfo(null, "ADSGAvgRot = " + _measureResult.ADSGAvgRot.AsString(" "));
            Logger.LogInfo(null, "Kgy = " + kGyro.AsString(" "));

            return new CalibrationFileEntity[]
            {
                CalibrationFileEntity.CreateArray("AH1", DataTypes.UINT16, _measureResult.ADH1Avg.ChangeType<ushort, double>()),
                CalibrationFileEntity.CreateArray("AH2", DataTypes.UINT16, _measureResult.ADH2Avg.ChangeType<ushort, double>()),
                CalibrationFileEntity.CreateArray("ASG", DataTypes.UINT16, _measureResult.ADSGAvg.ChangeType<ushort, double>()),
                CalibrationFileEntity.CreateArray("Kgy", DataTypes.FLOAT, kGyro.ChangeType<float, double>())
            };
        }

        protected override void discardCalibration()
        {
            _measureResult.Discard();
            OnPropertyChanged(nameof(CallibrationCanBeGenerated));
            _allPackets.Clear();
            _averagingCollection?.Clear();
        }
    }
}
