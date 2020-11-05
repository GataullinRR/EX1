//#define DECREASE_NUM_OF_POSITIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using DeviceBase.Devices;
using Vectors;
using System.IO;
using System.ComponentModel;
using TinyConfig;
using Calibrators.ViewModels;
using Calibrators.Models.Inclinometr;
using DeviceBase.Models;

namespace Calibrators.Models
{
    class InclinometrAngularCalibrator
        : InclinometrCalibratorBase<
            InclinometrAngularCalibrator.PrecisePosition,
            InclinometrAngularCalibrator.MeasureResult>
    {
        readonly static ConfigAccessor CONFIG2 = Configurable
            .CreateConfig("InclinometrAngularCalibrator");
        readonly static ConfigProxy<int> NUM_OF_POINTS_TO_ACCUMULATE = CONFIG2.Read(20);

        public enum Position
        {
            INC90_AZI0_GTF0,
            INC90_AZI45_GTF0,
            INC90_AZI90_GTF0,
#if !DECREASE_NUM_OF_POSITIONS
            INC90_AZI135_GTF0,
            INC90_AZI180_GTF0,
            INC90_AZI225_GTF0,
            INC90_AZI270_GTF0,
            INC90_AZI315_GTF0,
            INC90_AZI315_GTF45,
            INC90_AZI270_GTF45,
            INC90_AZI225_GTF45,
            INC90_AZI180_GTF45,
            INC90_AZI135_GTF45,
            INC90_AZI0_GTF90,
            INC90_AZI90_GTF90,
            INC90_AZI135_GTF90,
            INC90_AZI225_GTF90,
            INC90_AZI270_GTF90,
            INC90_AZI315_GTF135,
            INC90_AZI45_GTF135,
            INC90_AZI90_GTF180,
            INC90_AZI180_GTF180,
            INC90_AZI315_GTF225,
            INC90_AZI270_GTF225,
            INC90_AZI315_GTF315,
            INC90_AZI270_GTF315,
            INC135_AZI270_GTF315,
            INC135_AZI225_GTF270,
            INC135_AZI315_GTF180,
            INC135_AZI0_GTF180,
            INC135_AZI135_GTF135,
            INC135_AZI315_GTF90,
            INC45_AZI135_GTF45,
            INC45_AZI90_GTF180,
            INC45_AZI135_GTF180,
            INC45_AZI135_GTF270,
            INC45_AZI225_GTF315,
            INC15_AZI270_GTF225,
            INC15_AZI315_GTF180,
            INC15_AZI270_GTF180
#endif
        }

        public class PrecisePosition : INotifyPropertyChanged
        {
            readonly double[] _values;

            public event PropertyChangedEventHandler PropertyChanged;

            public Position Position { get; }
            public double Inc => _values[0] + IncOffset;
            public double Azi => _values[1] + AziOffset;
            public double Gtf => _values[2] + GtfOffset;
            public double IncOffset { get; set; }
            public double AziOffset { get; set; }
            public double GtfOffset { get; set; }

            PrecisePosition(Position position, double incOffset, double aziOffset, double gtfOffset)
            {
                Position = position;
                IncOffset = incOffset;
                AziOffset = aziOffset;
                GtfOffset = gtfOffset;

                var angles = position.GetAngles();
                _values = new[] { angles.Inc, angles.Azi, angles.GTF };
            }

            public static PrecisePosition FromAbsolute(
                Position position, double inc, double azi, double gtf)
            {
                var p = new PrecisePosition(position, 0, 0, 0);
                p.IncOffset = inc - p._values[0];
                p.AziOffset = azi - p._values[1];
                p.GtfOffset = gtf - p._values[2];

                return p;
            }

            public static PrecisePosition FromOffsets(
                Position position, double incOffset, double aziOffset, double gtfOffset)
            {
                var p = new PrecisePosition(position, incOffset, aziOffset, gtfOffset);

                return p;
            }

            public override string ToString()
            {
                return $"INC={_values[0]} AZI={_values[1]} GTF={_values[2]}";
            }
        }

        public class PositionMeasureResult
        {
            public PrecisePosition Position { get; }
            public IList<IEnumerable<double>> Result { get; }

            public PositionMeasureResult(PrecisePosition position, IList<IEnumerable<double>> result)
            {
                Position = position ?? throw new ArgumentNullException(nameof(position));
                Result = result ?? throw new ArgumentNullException(nameof(result));
            }
        }

        public class MeasureResult : MeasureResultBase
        {
            const string TIME_FORMAT = "yyyy-MM-dd HH-mm";

            public DateTime MeasureDateTime { get; }
            public IEnumerable<string> Columns { get; }
            public IEnumerable<PositionMeasureResult> Positions { get; }

            public MeasureResult(
                DateTime measureDateTime,
                IEnumerable<string> columns,
                IEnumerable<PositionMeasureResult> positions)
            {
                MeasureDateTime = measureDateTime;
                Columns = columns ?? throw new ArgumentNullException(nameof(columns));
                Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            }

            public override async Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync()
            {
                return await Task.Run(() => (generateFileName(), generateContent()));

                string generateFileName()
                {
                    return $"{MeasureDateTime.ToString(TIME_FORMAT)} InGK SENS raw.csv";
                }

                IEnumerable<byte> generateContent()
                {
                    var ms = new MemoryStream();
                    var writer = new StreamWriter(ms, Encoding.GetEncoding(CSV_ENCODING));
                    var rows = new Enumerable<IEnumerable<string>>
                    {
                        Columns,
                        Positions.Select((r, i) => getRows(i, r)).Flatten()
                    };
                    foreach (var row in rows)
                    {
                        writer.WriteLine(row.Aggregate(CSV_SEPARATOR));
                    }
                    writer.Flush();

                    return ms.ToArray();

                    IEnumerable<IEnumerable<string>> getRows(int i, PositionMeasureResult position)
                    {
                        yield return new string[]
                        {
                            "Position",
                            (i + 1).ToStringInvariant(),
                            position.Position.Inc.ToStringInvariant(),
                            position.Position.Azi.ToStringInvariant(),
                            position.Position.Gtf.ToStringInvariant()
                        };

                        foreach (var measure in position.Result)
                        {
                            yield return measure.Select(m => m.ToStringInvariant());
                        }
                    }
                }
            }

            public static async Task<MeasureResult> DeserializeAsync(string fileName, IEnumerable<byte> content)
            {
                await ThreadingUtils.ContinueAtThreadPull();

                var eb = new ExceptionBuilder();
                try
                {
                    var date = CommonUtils.TryOrDefault(
                        () => DateTime.ParseExact(fileName.Split(" InGK ").FirstElement(), TIME_FORMAT, null));

                    eb.SetException("Не удалось прочитать файл. Формат файла некорректен.");
                    var reader = new StreamReader(content.ToMemoryStream(), Encoding.GetEncoding(CSV_ENCODING));
                    var columns = reader
                        .ReadAllLines()
                        .FirstItem()
                        .Split(CSV_SEPARATOR);
                    var rows = parsePositionMeasures(reader).ToArray();

                    return new MeasureResult(date, columns, rows);
                }
                catch (Exception ex)
                {
                    throw eb.InstantiateException(ex);
                }

                IEnumerable<PositionMeasureResult> parsePositionMeasures(StreamReader reader)
                {
                    var lines = reader.ReadAllLines().ToArray().StartEnumeration();
                    lines.AdvanceOrThrow();

                    while (true)
                    {
                        var one = parseOne();
                        if (one == null)
                        {
                            yield break;
                        }
                        else
                        {
                            yield return one;
                        }
                    }

                    PositionMeasureResult parseOne()
                    {
                        if (lines.IsFinished)
                        {
                            return null;
                        }
                        var line = lines.Current;
                        if (line == null)
                        {
                            return null;
                        }
                        else if (!line.StartsWith("Position"))
                        {
                            throw new Exception();
                        }

                        var serializedPosition = line
                            .Split(CSV_SEPARATOR)
                            .Skip(2)
                            .Take(3)
                            .Select(v => v.ParseToDoubleInvariant())
                            .ToArray();
                        var p = new V3(serializedPosition[0], serializedPosition[1], serializedPosition[2]);
                        var positionInfo = (from pos in EnumUtils.GetValues<Position>()
                                            let angles = pos.GetAngles()
                                            let v3 = new V3(angles.Inc, angles.Azi, angles.GTF)
                                            let dV = v3 - p
                                            select (DDistance: dV, Position: pos))
                                            .OrderBy(v => v.DDistance.Mag)
                                            .FirstItem().Position;
                        var position = PrecisePosition.FromAbsolute(
                            positionInfo,
                            p.X,
                            p.Y,
                            p.Z);
                        var positionMeasure = new PositionMeasureResult(
                            position,
                            new List<IEnumerable<double>>());

                        foreach (var l in lines.AdvanceRange())
                        {
                            if (l.StartsWith("Position"))
                            {
                                break;
                            }
                            var measures = l
                                .Split(CSV_SEPARATOR)
                                .Where(v => v.IsNotNullOrWhiteSpace())
                                .Select(v => v.ParseToDoubleInvariant())
                                .ToArray();
                            positionMeasure.Result.Add(measures);
                        }

                        return positionMeasure;
                    }
                }
            }
        }

        public override EnhancedObservableCollection<MeasureResult> Results { get; } 
            = new EnhancedObservableCollection<MeasureResult>();
        readonly EnhancedObservableCollection<PositionMeasureResult> _measuredPositions 
            = new EnhancedObservableCollection<PositionMeasureResult>();
        readonly (string ResultsFileMnemonicName, MnemonicInfo<IncMnemonicType> DeviceMnemonic)[] _measureResultMnemonics;
        readonly IProgress<double> _totalProgress;

        protected override InclinometrMode modeDuringCalibration => InclinometrMode.ANGULAR_CALIBRATION;
        public override bool CallibrationCanBeGenerated => Results.Any();
        public PrecisePosition ActualPosition { get; private set; }

        protected override int requestInterval => 200;

        public InclinometrAngularCalibrator(IRUSDevice device, IRichProgress totalProgress) : base(device)
        {
            _totalProgress = totalProgress;
            totalProgress.MaxProgress = EnumUtils.GetValues<Position>().Count();

            _measureResultMnemonics = new(string, MnemonicInfo<IncMnemonicType>)[]
            {
                ( "INC", _mnemonics.Single(m => m.Type == IncMnemonicType.INC) ),
                ( "AZI", _mnemonics.Single(m => m.Type == IncMnemonicType.AZI) ),
                ( "GTF", _mnemonics.Single(m => m.Type == IncMnemonicType.GTF) ),
                ( "GX", _mnemonics.Single(m => m.Type == IncMnemonicType.GX) ),
                ( "GY", _mnemonics.Single(m => m.Type == IncMnemonicType.GY) ),
                ( "GZ", _mnemonics.Single(m => m.Type == IncMnemonicType.GZ) ),
                ( "BX", _mnemonics.Single(m => m.Type == IncMnemonicType.BX) ),
                ( "BY", _mnemonics.Single(m => m.Type == IncMnemonicType.BY) ),
                ( "BZ", _mnemonics.Single(m => m.Type == IncMnemonicType.BZ) ),
                ( "GXa", null ),
                ( "GYa", null ),
                ( "GZa", null ),
                ( "INCa", null ),
                ( "GTFa", null ),
                ( "GTOTa", null ),
                ( "TempGx", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
                ( "TempGy", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
                ( "TempGz", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
            };

            _measuredPositions.CollectionChanged += (o, e) => OnPropertyChanged(null);
            Results.CollectionChanged += (o, e) => OnPropertyChanged(null);
        }

        public bool IsPositionMeasured(Position position)
        {
            var mp = _measuredPositions.Find(p => p.Position.Position == position);

            return mp.Found
                ? mp.Value.Result.Count == NUM_OF_POINTS_TO_ACCUMULATE
                : Results.SingleOrDefault()?.Positions?.Any(p => p.Position.Position == position) ?? false;
        }

        protected override async Task onMeasureAsync(PrecisePosition mode)
        {
            _measuredPositions.Remove(mp => mp.Position.Position == mode.Position);

            await TaskUtils.CompletedTask;
        }

        protected override async Task finishAsync(PrecisePosition mode, DateTime measureStart)
        {
            Results.Clear();

            var isFinished = _measuredPositions
                .Select(mp => mp.Position.Position)
                .ContainsAll(EnumUtils.GetValues<Position>());
            if (isFinished)
            {
                var measureResult = new MeasureResult(
                    measureStart,
                    _measureResultMnemonics.Select(i => i.ResultsFileMnemonicName),
                    _measuredPositions);
                Results.Add(measureResult);
            }

            ActualPosition = null;

            await TaskUtils.CompletedTask;
        }
        protected override void onMeasureFinallized(PrecisePosition mode)
        {
            if (State != MeasureState.FINISHED_SUCCESSFULLY)
            {
                // Remove unfinished measure
                _measuredPositions.Remove(p => p.Position.Position == mode.Position);
            }

            _totalProgress.Report(Results.Any() 
                ? Results.Single().Positions.Count() 
                : _measuredPositions.Count);
        }
        protected override async Task handleDataPacketAsync(
            PrecisePosition mode, 
            IProgress<double> progress, 
            IEnumerable<KnownEntity> entities)
        {
            var results = new List<double>();
            foreach (var info in _measureResultMnemonics)
            {
                if (info.DeviceMnemonic == null)
                {
                    results.Add(0);
                }
                else
                {
                    var entity = entities.Single(e => e.Entity.Descriptor.Name == info.DeviceMnemonic.DeviceMnemonic);
                    results.Add(entity.Value);
                }
            }

            ActualPosition = PrecisePosition.FromAbsolute(mode.Position, results[0], results[1], results[2]);

            // Is first measured point
            if (!_measuredPositions.Any(p => p.Position.Position == mode.Position))
            {
                var resultsArr = new DisplaceCollection<IEnumerable<double>>(NUM_OF_POINTS_TO_ACCUMULATE);
                _measuredPositions.Add(new PositionMeasureResult(mode, resultsArr));
            }
            var measures = _measuredPositions.Single(p => p.Position.Position == mode.Position).Result;
            measures.Add(results);

            progress.Report((double)measures.Count / NUM_OF_POINTS_TO_ACCUMULATE);
            if (measures.Count >= NUM_OF_POINTS_TO_ACCUMULATE)
            {
                setWaitingForFinish();
            }

            await TaskUtils.CompletedTask;
        }

        protected override async Task<IEnumerable<CalibrationFileEntity>> generateCalibrationCoefficients(IEnumerable<Curve> curves)
        {
            var kx = curves.Single(c => c.Name == "Kx").Points.Select(v => v.ToSingle());
            var ky = curves.Single(c => c.Name == "Ky").Points.Select(v => v.ToSingle());
            var kz = curves.Single(c => c.Name == "Kz").Points.Select(v => v.ToSingle());
            var m0 = curves.Single(c => c.Name == "MRow0").Points.Select(v => v.ToSingle());
            var m1 = curves.Single(c => c.Name == "MRow1").Points.Select(v => v.ToSingle());
            var m2 = curves.Single(c => c.Name == "MRow2").Points.Select(v => v.ToSingle());

            await TaskUtils.CompletedTask;

            return getEntities();

            IEnumerable<CalibrationFileEntity> getEntities()
            {
                yield return CalibrationFileEntity.CreateInclinometrAccelerometrChannelAngularCalibrationTable(
                    "Kx", DataTypes.FLOAT, kx);
                yield return CalibrationFileEntity.CreateInclinometrAccelerometrChannelAngularCalibrationTable(
                    "Ky", DataTypes.FLOAT, ky);
                yield return CalibrationFileEntity.CreateInclinometrAccelerometrChannelAngularCalibrationTable(
                    "Kz", DataTypes.FLOAT, kz);
                yield return CalibrationFileEntity.CreateInclinometrMagnitometrChannelAngularCalibrationTable(
                    "Mx", DataTypes.FLOAT, m0);
                yield return CalibrationFileEntity.CreateInclinometrMagnitometrChannelAngularCalibrationTable(
                    "My", DataTypes.FLOAT, m1);
                yield return CalibrationFileEntity.CreateInclinometrMagnitometrChannelAngularCalibrationTable(
                    "Mz", DataTypes.FLOAT, m2);
            }
        }

        protected override void discardCalibration()
        {
            Results.Clear();
            _measuredPositions.Clear();
            _totalProgress.Report(0);
            ActualPosition = null;
            OnPropertyChanged(nameof(CallibrationCanBeGenerated));
        }
    }
}