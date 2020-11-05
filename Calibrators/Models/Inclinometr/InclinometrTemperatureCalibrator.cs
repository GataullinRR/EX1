using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities;
using Utilities.Extensions;
using System.Text;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Vectors;
using DeviceBase;
using MVVMUtilities.Types;
using TinyConfig;
using SpecificMarshallers;
using DeviceBase.IOModels;
using DeviceBase.Devices;
using Common;
using Calibrators.ViewModels.Inclinometr;

namespace Calibrators.Models
{
    class InclinometrTemperatureCalibrator : InclinometrCalibratorBase<
            InclinometrTemperatureCalibrator.IncTCalAngle,
            InclinometrTemperatureCalibrator.MeasureResult>
    {
        readonly static IncMnemonicType[] TEMPERATURE_MNEMONICS = new IncMnemonicType[]
        {
            IncMnemonicType.ACCELEROMETR_TEMPERATURE,
            IncMnemonicType.MAGNITOMETR_TEMPERATURE
        };

        readonly static ConfigAccessor CONFIG2 = Configurable
            .CreateConfig("InclinometrTemperatureCalibrator")
            .AddMarshaller<TryParseMarshaller<Interval>>();
        readonly static ConfigProxy<Interval> TEMPERATURE_RANGE = CONFIG2.Read(new Interval(30, 125));

        static InclinometrTemperatureCalibrator()
        {

        }

        public enum IncTCalAngle
        {
            INC0_AZI0_GTF0,
            INC45_AZI45_GTF45,
            INC135_AZI315_GTF225,
            [TestAngle]
            INC60_AZI60_GTF60,
        }

        public class MeasureResult : MeasureResultBase
        {
            const string TIME_FORMAT = "yyyy-MM-dd HH-mm";

            public IncTCalAngle Mode { get; }
            public DateTime MeasureDateTime { get; }
            public string[] Columns { get; }
            public IEnumerable<double[]> Rows { get; }

            public MeasureResult(IncTCalAngle mode, DateTime measureDateTime, string[] columns, IEnumerable<double[]> rows)
            {
                Mode = mode;
                MeasureDateTime = new DateTime(measureDateTime.Year, measureDateTime.Month, measureDateTime.Day, measureDateTime.Hour, measureDateTime.Minute, 0);

                Columns = columns ?? throw new ArgumentNullException(nameof(columns));
                Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            }

            public override async Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync()
            {
                return await Task.Run(() => (generateFileName(), generateContent()));

                string generateFileName()
                {
                    var mode = Mode.ToAnglesString();
                    return $"{MeasureDateTime.ToString(TIME_FORMAT)} InGK TCAL raw {mode}.csv";
                }

                IEnumerable<byte> generateContent()
                {
                    var ms = new MemoryStream();
                    var writer = new StreamWriter(ms, Encoding.GetEncoding(CSV_ENCODING));
                    var rows = new Enumerable<string[]>
                    {
                        Columns,
                        Rows.Select(r => r.Select(c => c.ToStringInvariant()).ToArray())
                    };
                    foreach (var row in rows)
                    {
                        writer.WriteLine(row.Aggregate(CSV_SEPARATOR));
                    }
                    writer.Flush();

                    return ms.ToArray();
                }
            }

            public static async Task<MeasureResult> DeserializeAsync(string fileName, IEnumerable<byte> content)
            {
                await ThreadingUtils.ContinueAtThreadPull();

                var eb = new ExceptionBuilder();
                try
                {
                    eb.SetException("Не удалось прочитать параметры калибровки из имени файла. Имя файла некорректно.");
#warning move to attribute
                    var mode = EnumUtils.GetValues<IncTCalAngle>().First(v => fileName.Contains(v.ToAnglesString()));
                    var date = DateTime.ParseExact(fileName.Split(" InGK ").FirstElement(), TIME_FORMAT, null);

                    eb.SetException("Не удалось прочитать файл. Формат файла некорректен.");
                    var reader = new StreamReader(content.ToMemoryStream(), Encoding.GetEncoding(CSV_ENCODING));
                    var columns = reader.ReadAllLines().FirstItem().Split(CSV_SEPARATOR);
                    var rows = parseRows(reader).ToArray();

                    return new MeasureResult(mode, date, columns, rows);
                }
                catch (Exception ex)
                {
                    throw eb.InstantiateException(ex);
                }

                IEnumerable<double[]> parseRows(StreamReader reader)
                {
                    foreach (var line in reader.ReadAllLines())
                    {
                        yield return line
                            .Split(CSV_SEPARATOR)
                            .Select(v => v.ParseToDoubleInvariant())
                            .ToArray();
                    }
                }
            }

            public override bool Equals(object obj)
            {
                var result = obj as MeasureResult;
                return result != null &&
                       Mode == result.Mode &&
                       MeasureDateTime == result.MeasureDateTime &&
                       Columns.SequenceEqual(result.Columns) &&
                       Rows.Flatten().SequenceEqual(result.Rows.Flatten());
            }

            public override int GetHashCode()
            {
                var hashCode = -1080134904;
                hashCode = hashCode * -1521134295 + Mode.GetHashCode();
                hashCode = hashCode * -1521134295 + MeasureDateTime.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Columns);
                hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<double[]>>.Default.GetHashCode(Rows);
                return hashCode;
            }
        }

        class MnemonicInfo
        {
            public IncMnemonicType Type { get; }
            public string DeviceMnemonic { get; }
            public string CalibratorMnemonic { get; }
            public Func<dynamic, double> Decode { get; }
            public Func<dynamic, bool> IsInvalidValue { get; }

            public MnemonicInfo(IncMnemonicType type, string deviceMnemonic, string calibratorMnemonic, Func<dynamic, double> decode, Func<dynamic, bool> isInvalidValue)
            {
                Type = type;
                DeviceMnemonic = deviceMnemonic;
                CalibratorMnemonic = calibratorMnemonic ?? throw new ArgumentNullException(nameof(calibratorMnemonic));
                Decode = decode ?? throw new ArgumentNullException(nameof(decode));
                IsInvalidValue = isInvalidValue ?? throw new ArgumentNullException(nameof(isInvalidValue));
            }
        }

        public enum States
        {
            WAITING_FOR_START = 0,
            WAITING_FOR_HEATING_UP,
            WAITING_FOR_COOLING_DOWN,
            WAITING_FOR_FINISH,
            FINISHED_WITH_ERROR,
            FINISHED_SUCCESSFULLY,
            CANCELLED,
            WAITING_FOR_CALIBRATION_MODE_SET
        }

        public class DeviceParameters
        {
            public DeviceParameters()
                : this(0,0,0,0,0)
            {

            }
            public DeviceParameters(double gyroTemperature, double pCBTemperature, double actualInclination, double actualAzimuth, double actualGravity)
            {
                GyroTemperature = gyroTemperature;
                PCBTemperature = pCBTemperature;
                Inclination = actualInclination;
                Azimuth = actualAzimuth;
                Gravity = actualGravity;
            }

            public double GyroTemperature { get; }
            public double PCBTemperature { get; }
            public double Inclination { get; }
            public double Azimuth { get; }
            public double Gravity { get; }
        }

        readonly List<KnownEntity[]> _lastMeasureRows = new List<KnownEntity[]>();
        readonly (string ResultsFileMnemonicName, MnemonicInfo<IncMnemonicType> DeviceMnemonic)[] _measureResultMnemonics;

        protected override int requestInterval => 200;
        protected override InclinometrMode modeDuringCalibration => InclinometrMode.TEMPERATURE_CALIBRATION;
        public DeviceParameters ActualDeviceParameters { get; private set; } = new DeviceParameters();

        public States CalibrationState { get; private set; }
        public override bool CallibrationCanBeGenerated => Results.Select(r => r.Mode).OrderBy()
                .SequenceEqual(EnumUtils.GetValues<IncTCalAngle>().OrderBy());

        public override EnhancedObservableCollection<MeasureResult> Results { get; }
            = new EnhancedObservableCollection<MeasureResult>();
        public Interval TemperatureRange => TEMPERATURE_RANGE;

        public InclinometrTemperatureCalibrator(IRUSDevice device)
            : base(device)
        {
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
                ( "TempGx", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
                ( "TempGy", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
                ( "TempGz", _mnemonics.Single(m => m.Type == IncMnemonicType.ACCELEROMETR_TEMPERATURE) ),
                ( "Температура ИнГК", _mnemonics.Single(m => m.Type == IncMnemonicType.MAGNITOMETR_TEMPERATURE) ),
            };

            _propertyHolder.CreateExternalDependency(nameof(CallibrationCanBeGenerated), Results);
        }

        protected override Task onSingleMeasureAsync(IncTCalAngle mode)
        {
            switch (CalibrationState)
            {
                case States.WAITING_FOR_HEATING_UP:
                    CalibrationState = getLastTemperaturesOrNull()?.Min() >= TemperatureRange.To
                        ? States.WAITING_FOR_COOLING_DOWN
                        : CalibrationState;
                    break;

                case States.WAITING_FOR_COOLING_DOWN:
                    CalibrationState = getLastTemperaturesOrNull()?.Max() <= TemperatureRange.From
                        ? States.WAITING_FOR_FINISH
                        : CalibrationState;
                    break;

                case States.WAITING_FOR_START:
                    CalibrationState = States.WAITING_FOR_HEATING_UP;
                    break;

                case States.FINISHED_WITH_ERROR:
                case States.FINISHED_SUCCESSFULLY:
                case States.CANCELLED:
                    CalibrationState = States.WAITING_FOR_HEATING_UP;
                    break;

                case States.WAITING_FOR_FINISH:
                    setWaitingForFinish();
                    break;
                case States.WAITING_FOR_CALIBRATION_MODE_SET:

                    break;

                default:
                    throw new NotSupportedException();
            }

            return Task.FromResult(true);
        }
        double[] getLastTemperaturesOrNull()
        {
            return _lastMeasureRows.Count == 0
                ? null
                : getTemperatures(_lastMeasureRows.LastElement());
        }
        double[] getTemperatures(KnownEntity[] entities)
        {
            return entities
                .NullToEmpty()
                .SkipNulls()
                .Where(e => e.Info.Type.IsOneOf(TEMPERATURE_MNEMONICS))
                .Select(e => e.Value)
                .ToArray();
        }

        protected override void onMeasureFinallized(IncTCalAngle mode)
        {
            switch (State)
            {
                case MeasureState.FINISHED_WITH_ERROR:
                    CalibrationState = States.FINISHED_WITH_ERROR;
                    break;
                case MeasureState.FINISHED_SUCCESSFULLY:
                    CalibrationState = States.FINISHED_SUCCESSFULLY;
                    break;
                case MeasureState.CANCELLED:
                    CalibrationState = States.CANCELLED;
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        protected override Task onMeasureAsync(IncTCalAngle mode)
        {
            _lastMeasureRows.Clear();
            ActualDeviceParameters = new DeviceParameters(0, 0, 0, 0, 0);

            return Task.FromResult(true);
        }

        protected override async Task finishAsync(IncTCalAngle mode, DateTime measureStart)
        {
            var header = "N"
                .ToSequence()
                .Concat(_measureResultMnemonics.Select(m => m.ResultsFileMnemonicName)).ToArray();
            var result = new MeasureResult(mode, measureStart, header, await getFixedRowsAsync());
            Results.Remove(r => r.Mode == mode);
            Results.Add(result);
            CalibrationState = States.FINISHED_SUCCESSFULLY;

            async Task<double[][]> getFixedRowsAsync()
            {
                await ThreadingUtils.ContinueAtThreadPull();

                var rows = _lastMeasureRows.Select(row => row.Select(e => e?.Value ?? 0D).ToArray()).ToArray();
                var temperatureMnemonicIndexes = TEMPERATURE_MNEMONICS
                    .Select(tM => _lastMeasureRows.LastElement().Find(e => e?.Info?.Type == tM).Index)
                    .ToArray();
                foreach (var tMI in temperatureMnemonicIndexes)
                {
                    var maxT = rows.Max(r => r[tMI]);
                    var maxTIndex = rows.Find(r => r[tMI] == maxT).Index;
                    for (int i = 0; i < maxTIndex; i++)
                    {
                        rows[i].SetAll(double.NaN);
                    }
                    var interpolationIntervals = getInterpolationIntervals(maxTIndex, tMI);
                    for (int i = interpolationIntervals.LastElement().To + 1; i < rows.Length; i++)
                    {
                        rows[i].SetAll(double.NaN);
                    }
                    foreach (var interval in interpolationIntervals)
                    {
                        var tInterval = new Interval(rows[interval.From][tMI], rows[interval.To][tMI]);
                        var tStep = tInterval.Len / interval.Len;
                        for (int i = interval.From, stepI = 0; i < interval.To; i++, stepI++)
                        {
                            rows[i][tMI] = rows[interval.From][tMI] + tStep * stepI;
                        }
                    }

                    var selectedRows = new IntInterval(interpolationIntervals[0].From, interpolationIntervals.LastElement().To);
                    var curveType = _lastMeasureRows.LastElement()[tMI].Info.Type;
                    var sameCurveIndexes = _lastMeasureRows.LastElement()
                        .FindAllIndexes(e => e?.Info?.Type == curveType);
                    foreach (var ci in sameCurveIndexes)
                    {
                        for (int i = selectedRows.From; i < selectedRows.To; i++)
                        {
                            rows[i][ci] = rows[i][tMI];
                        }
                    }

                    Logger.LogInfo(null, $"Завершено исправление по кривой {curveType} Tmin: {rows[interpolationIntervals.LastElement().To][tMI]} Tmax: {rows[interpolationIntervals[0].From][tMI]} NumOfInterpolationIntervals: {interpolationIntervals.Count} RowsSelected: {selectedRows:L}");
                }

                return rows
                    .Where(r => r.FirstElement().IsNotNaN())
                    .Select((row, i) => new Enumerable<double> { i + 1, row }.ToArray())
                    .ToArray();

                ////////////////////////////////////////////////////////

                List<IntInterval> getInterpolationIntervals(int maxTIndex, int tempMnemonicIndex)
                {
                    var interpolationIntervals = new List<IntInterval>();
                    var prevTIndex = maxTIndex;
                    var prevT = rows[prevTIndex][tempMnemonicIndex];
                    for (int i = maxTIndex + 1; i < rows.Length; i++)
                    {
                        var currT = rows[i][tempMnemonicIndex];
                        if (currT < prevT)
                        {
                            interpolationIntervals.Add(new IntInterval(prevTIndex, i));
                            prevTIndex = i;
                            prevT = currT;
                        }
                    }

                    return interpolationIntervals;
                }
            }
        }

        protected override Task handleDataPacketAsync(IncTCalAngle mode, IProgress<double> progress, IEnumerable<KnownEntity> entities)
        {
            _lastMeasureRows.Add(new KnownEntity[_measureResultMnemonics.Length]);
            foreach (var mi in _measureResultMnemonics.Length.Range())
            {
                var m = _measureResultMnemonics[mi];
                if (m.DeviceMnemonic != null)
                {
                    var entity = entities.Find(e => e.Entity.Descriptor.Name == m.DeviceMnemonic.DeviceMnemonic);
                    _lastMeasureRows.LastElement()[mi] = entity.Value;
                }
            }

            var deviceParams = _measureResultMnemonics.GetCorresponding(
                info => info.DeviceMnemonic?.Type,
                IncMnemonicType.ACCELEROMETR_TEMPERATURE, IncMnemonicType.MAGNITOMETR_TEMPERATURE, IncMnemonicType.INC, IncMnemonicType.AZI, IncMnemonicType.GTF)
                .Select(info => entities.Find(e => e.Entity.Descriptor.Name == info.DeviceMnemonic.DeviceMnemonic).Value.Value)
                .ToArray();
            ActualDeviceParameters = new DeviceParameters(deviceParams[0], deviceParams[1], deviceParams[2], deviceParams[3], deviceParams[4]);

            return Task.FromResult(true);
        }

        protected override async Task<IEnumerable<CalibrationFileEntity>> generateCalibrationCoefficients(IEnumerable<Curve> curves)
        {
            var calibrationCoefficients = new List<CalibrationFileEntity>();

            curves = curves.Where(c => " VG1x; KG1x; VG1y; KG1y; VG1z; KG1z; VM1x; KM1x; VM1y; KM1y; VM1z; KM1z;".Split(";").Contains(c.Name));
            if (curves.Count() != 12)
            {
                throw new Exception("Некорректный формат файла калибровок. Отутсвуют необходимые мнемоники.");
            }
            // y = K*x + B
            var bkCoefficients = curves
                .GroupBy(2)
                .Select(g => (B: g.FirstItem(), K: g.LastItem()));
            foreach (var curve in bkCoefficients)
            {
                var value = CalibrationFileEntity.CreateLinearInterpolationTable(
                    curve.B.Name.Skip(2).Aggregate(),
                    DataTypes.FLOAT,
                    curve.B.Points.Zip(curve.K.Points, (b, k) => ((float)b, (float)k)));
                calibrationCoefficients.Add(value);
            }

            return calibrationCoefficients;
        }

        protected override void discardCalibration()
        {
            Results.Clear();
            ActualDeviceParameters = new DeviceParameters();
            _lastMeasureRows.Clear();
            CalibrationState = default;
            OnPropertyChanged(nameof(CallibrationCanBeGenerated));
        }
    }
}
