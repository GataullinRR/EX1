using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.IO;
using System.Diagnostics;
using Calibrators.ViewModels.Inclinometr;
using Vectors;

namespace Calibrators.Models
{
    class CalibratorApplication
    {
        readonly static string CALIBRATOR_ROOT_PATH = Path.Combine(Environment.CurrentDirectory, "InclinometrCalibrator");
        readonly static string CALIBRATOR_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "IncCallibration.exe");
        readonly static string TEMPERATURE_CALIBRATION_FILE_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "TCalibrationCoefficients.cal");
        readonly static string ACCELEROMETR_ANGLULAR_CALIBRATION_FILE_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "Matrix_Accel_Set_0.txt");
        readonly static string MAGNITOMETR_ANGLULAR_CALIBRATION_FILE_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "Matrix_Magn_Set.txt");
        readonly static string MEASURE_RESULTS_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "MeasureResults");
        readonly static string CONFIG_FILE_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "InFile.txt");
        readonly static string CONSTANTS_FILE_PATH = Path.Combine(CALIBRATOR_ROOT_PATH, "GEO_constants.txt");

        class ConfigBuilder
        {
            readonly StringBuilder _sb = new StringBuilder();

            public string Config => _sb.ToString();

            ConfigBuilder() { }

            public static string CreateTemperatureCalibrationConfig(
                string calibrationFileName, 
                IEnumerable<string> measureResultsPaths)
            {
                var config = new ConfigBuilder();
                config._sb.AppendLine("4");
                config._sb.AppendLine(calibrationFileName);
                measureResultsPaths.ForEach(addMeasureResultsPath);

                return config.Config;

                void addMeasureResultsPath(string measureResultsPath)
                {
                    config._sb.AppendLine(measureResultsPath.Replace('\\', '/'));
                }
            }

            public static string CreateAngularCalibrationConfig(
                string accelerometrCalibrationFileName,
                string magnitometrCalibrationFileName,
                string measureResultsPath)
            {
                var config = new ConfigBuilder();
                config._sb.AppendLine("1");
                config._sb.AppendLine(measureResultsPath);
                config._sb.AppendLine("SENSLIN_out.csv");
                config._sb.AppendLine(accelerometrCalibrationFileName);
                config._sb.AppendLine("Matrix_Accel_Set_1.txt");
                config._sb.AppendLine(magnitometrCalibrationFileName);

                return config.Config;
            }

            public static string CreateApplyTemperatureCalibrationConfig(
                string calibrationFileName,
                IEnumerable<string> measureResultsPaths)
            {
                var config = new ConfigBuilder();
                config._sb.AppendLine("5");
                config._sb.AppendLine(calibrationFileName);
                measureResultsPaths.ForEach(addMeasureResultsPath);

                return config.Config;

                void addMeasureResultsPath(string measureResultsPath)
                {
                    config._sb.AppendLine(measureResultsPath.Replace('\\', '/'));
                }
            }

            public static string CreateApplyAngularCalibrationConfig(
                string accelerometrCalibrationFileName,
                string magnitometrCalibrationFileName,
                string measureResultsPath, 
                string calibratedMeasurePath)
            {
                var config = new ConfigBuilder();
                config._sb.AppendLine("3");
                addMeasureResultsPath(measureResultsPath);
                addMeasureResultsPath(calibratedMeasurePath);
                addMeasureResultsPath(accelerometrCalibrationFileName);
                config._sb.AppendLine("Matrix_Accel_Set_1.txt");
                addMeasureResultsPath(magnitometrCalibrationFileName);

                return config.Config;

                void addMeasureResultsPath(string mrp)
                {
                    config._sb.AppendLine(mrp.Replace('\\', '/'));
                }
            }
        }

        enum CalibrationType
        {
            TEMPERATURE,
            ANGULAR
        }

        readonly CalibrationType _calibrationType;
        readonly IEnumerable<MeasureResultBase> _results;
        readonly CalibrationConstants _constants;

        public CalibratorApplication(IEnumerable<MeasureResultBase> results, CalibrationConstants constants)
        {
            _results = results;
            _constants = constants;

            if (results is IEnumerable<InclinometrTemperatureCalibrator.MeasureResult>)
            {
                _calibrationType = CalibrationType.TEMPERATURE;
            }
            else if (results is IEnumerable<InclinometrAngularCalibrator.MeasureResult>)
            {
                _calibrationType = CalibrationType.ANGULAR;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IEnumerable<Curve>> CalculateCoefficientsAsync(bool useTestAngle = true)
        {
            await ThreadingUtils.ContinueAtThreadPull();

            await setup();
            run();
            return getResults().MakeCached();

            async Task setup()
            {
                IOUtils.RecreateDirectory(MEASURE_RESULTS_PATH);
                var measureResultsPaths = new List<string>();
                var mr = !useTestAngle && _calibrationType == CalibrationType.TEMPERATURE
                    ? _results
                        .Cast<InclinometrTemperatureCalibrator.MeasureResult>()
                        .Where(r => r.Mode.GetAttribute<TestAngleAttribute>() == null)
                    : _results;
                foreach (var measureResult in mr)
                {
                    var serialized = await measureResult.SerializeAsync();
                    var path = Path.Combine(MEASURE_RESULTS_PATH, serialized.FileName);
                    measureResultsPaths.Add(path);

                    IOUtils.CreateFile(path).WriteAndDispose(serialized.Content);
                }

                IOUtils.CreateFile(CONFIG_FILE_PATH)
                    .ToStreamWriter(Encoding.ASCII)
                    .WriteAndDispose(generateConfig());
                IOUtils.CreateFile(CONSTANTS_FILE_PATH)
                    .ToStreamWriter(Encoding.ASCII)
                    .WriteAndDispose(_constants.GenerateFile());

                string generateConfig()
                {
                    switch (_calibrationType)
                    {
                        case CalibrationType.TEMPERATURE:
                            return ConfigBuilder.CreateTemperatureCalibrationConfig(
                                Path.GetFileName(TEMPERATURE_CALIBRATION_FILE_PATH),
                                measureResultsPaths);
                        case CalibrationType.ANGULAR:
                            return ConfigBuilder.CreateAngularCalibrationConfig(
                                Path.GetFileName(ACCELEROMETR_ANGLULAR_CALIBRATION_FILE_PATH),
                                Path.GetFileName(MAGNITOMETR_ANGLULAR_CALIBRATION_FILE_PATH),
                                measureResultsPaths.Single());

                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            IEnumerable<Curve> getResults()
            {
                switch (_calibrationType)
                {
                    case CalibrationType.TEMPERATURE:
                        {
                            const string CURVE_SEPARATOR = ";";
                            var cells = File.ReadAllLines(TEMPERATURE_CALIBRATION_FILE_PATH)
                                .Select(l => l.Split(CURVE_SEPARATOR))
                                .To2DArray();
                            for (int curveI = 0; curveI < cells.GetColumnsLength(); curveI++)
                            {
                                var points = new List<double>();
                                var curve = new Curve(cells[curveI, 0], points);
                                for (int pointI = 1; pointI < cells.GetRowsLength(); pointI++)
                                {
                                    points.Add(cells[curveI, pointI].ParseToDoubleInvariant());
                                }

                                yield return curve;
                            }
                        }
                        break;
                    case CalibrationType.ANGULAR:
                        {
                            var kXkYxZ = parse(ACCELEROMETR_ANGLULAR_CALIBRATION_FILE_PATH);
                            yield return new Curve("Kx", kXkYxZ[0]);
                            yield return new Curve("Ky", kXkYxZ[1]);
                            yield return new Curve("Kz", kXkYxZ[2]);
                            var kM = parse(MAGNITOMETR_ANGLULAR_CALIBRATION_FILE_PATH);
                            yield return new Curve("MRow0", kM[0]);
                            yield return new Curve("MRow1", kM[1]);
                            yield return new Curve("MRow2", kM[2]);

                            IEnumerable<double>[] parse(string filePath)
                            {
                                return File.ReadAllLines(filePath)
                                    .Skip(1)
                                    .Select(l => l.Split(" ").ParseToDoubleInvariant())
                                    .ToArray();
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        void run()
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = CALIBRATOR_PATH;
            processStartInfo.WorkingDirectory = Path.GetDirectoryName(CALIBRATOR_PATH);
            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception("Расчет калибровочных коэффициентов завершился с ошибкой");
            }
        }

        public async Task<CalibratedPoint[][]> CalculateErrorsAsync(bool useTestAngle = true)
        {
            await ThreadingUtils.ContinueAtThreadPull();

            //It creates calibration file in the calibrator folder
            await CalculateCoefficientsAsync(useTestAngle);
            var correctionResults = await setupAsync();
            run();

            switch (_calibrationType)
            {
                case CalibrationType.TEMPERATURE:
                    {
                        var curves = new List<CalibratedPoint[]>();
                        foreach (var result in correctionResults)
                        {
                            var corrFileContent = File.ReadAllBytes(result.CorrFilePath);
                            var corrFile = await InclinometrTemperatureCalibrator.MeasureResult
                                .DeserializeAsync(Path.GetFileName(result.CorrFilePath), corrFileContent);

                            var angles = ((InclinometrTemperatureCalibrator.MeasureResult)result.Measure).Rows
                                .Select(row => new V3(row[1], row[2], row[3])) // INC, AZI, GTF
                                .ToArray();
                            var accelerometr = ((InclinometrTemperatureCalibrator.MeasureResult)result.Measure).Rows
                                .Select(row => new V3(row[4], row[5], row[6])) // GX Y Z
                                .ToArray();
                            var magnitometr = ((InclinometrTemperatureCalibrator.MeasureResult)result.Measure).Rows
                                .Select(row => new V3(row[7], row[8], row[9])) // BX Y Z
                                .ToArray();
                            var accelerometrTemperaturtes = ((InclinometrTemperatureCalibrator.MeasureResult)result.Measure).Rows
                                .Select(row => new V3(row[13], row[14], row[15])) // TempGx y z
                                .ToArray();
                            var magnitometrTemperature = ((InclinometrTemperatureCalibrator.MeasureResult)result.Measure).Rows
                                .Select(row => new V3(row[16], row[16], row[16]))
                                .ToArray();

                            var correctedAngles = corrFile.Rows
                                .Select(row => new V3(row[1], row[2], row[3])) // INC, AZI, GTF
                                .ToArray();
                            var correctedAccelerometr = corrFile.Rows
                                .Select(row => new V3(row[4], row[5], row[6])) // GX Y Z
                                .ToArray();
                            var correctedMagnitometr = corrFile.Rows
                                .Select(row => new V3(row[7], row[8], row[9])) // BX Y Z
                                .ToArray();

                            var temperatures = corrFile.Rows.Select(row => row.TakeFromEnd(2).Average()).ToArray();
                            var angle = corrFile.Mode.GetAngles();
                            var curve = angles.Length.Range()
                                .Select(i => new CalibratedPoint(
                                    angle,
                                    angles[i],
                                    accelerometr[i],
                                    magnitometr[i],
                                    correctedAngles[i],
                                    correctedAccelerometr[i],
                                    correctedMagnitometr[i],
                                    accelerometrTemperaturtes[i],
                                    magnitometrTemperature[i],
                                    temperatures[i]))
                                .ToArray();
                            curves.Add(curve);
                        }

                        return curves.ToArray();
                    }
                case CalibrationType.ANGULAR:
                    {
                        var curves = new List<CalibratedPoint[]>();
                        var result = correctionResults.Single();

                        var corrFileContent = File.ReadAllBytes(result.CorrFilePath);
                        var corrFile = await InclinometrAngularCalibrator.MeasureResult
                            .DeserializeAsync(Path.GetFileName(result.CorrFilePath), corrFileContent);
                        var points = new CalibratedPoint[corrFile.Positions.Count()][];
                        var i = 0;
                        foreach (var position in corrFile.Positions)
                        {
                            var angles = ((InclinometrAngularCalibrator.MeasureResult)result.Measure).Positions
                                .First(p => p.Position.Position == position.Position.Position)
                                .Result.Select(r => r.ToArray())
                                .Select(row => new V3(row[0], row[1], row[2])) // INC, AZI, GTF
                                .ToArray();
                            var correctedAngles = position.Result
                                .Select(r => r.ToArray())
                                .Select(row => new V3(row[0], row[1], row[2])) // INC, AZI, GTF
                                .ToArray();
                            var expectedAngles = position.Position.Position.GetAngles();
                            var expectedAnglesV3 = new V3(expectedAngles.Inc, expectedAngles.Azi, expectedAngles.GTF);

                            points[i] = angles.Length.Range()
                                .Select(k => new CalibratedPoint(expectedAnglesV3, angles[k], V3.Zero, V3.Zero, correctedAngles[k], V3.Zero, V3.Zero, V3.Zero, V3.Zero, 0))
                                .ToArray();
                            i++;
                        }

                        return points;
                    }

                default:
                    throw new NotSupportedException();
            }

            async Task<(MeasureResultBase Measure, string CorrFilePath)[]> setupAsync()
            {
                if (_calibrationType == CalibrationType.ANGULAR)
                {
                    return new[] { (_results.Single(), Path.Combine(CALIBRATOR_ROOT_PATH, "SENSLIN_out.csv")) };
                }

                IOUtils.RecreateDirectory(MEASURE_RESULTS_PATH);
                var measureResultsPaths = new List<string>();
                var results = _calibrationType == CalibrationType.TEMPERATURE
                    ? _results
                        .Cast<InclinometrTemperatureCalibrator.MeasureResult>()
                        .OrderBy(r => r.Mode)
                    : _results;
                var files = new List<(MeasureResultBase Measure, string CorrFilePath)>();
                foreach (var measureResult in results)
                {
                    var serialized = await measureResult.SerializeAsync();
                    var path = Path.Combine(MEASURE_RESULTS_PATH, serialized.FileName);
                    measureResultsPaths.Add(path);

                    IOUtils.CreateFile(path).WriteAndDispose(serialized.Content);

                    files.Add((measureResult, path.Replace(".csv", "_Corr.csv")));
                }

                IOUtils.CreateFile(CONFIG_FILE_PATH)
                    .ToStreamWriter(Encoding.ASCII)
                    .WriteAndDispose(generateConfig());
                IOUtils.CreateFile(CONSTANTS_FILE_PATH)
                    .ToStreamWriter(Encoding.ASCII)
                    .WriteAndDispose(_constants.GenerateFile());

                string generateConfig()
                {
                    switch (_calibrationType)
                    {
                        case CalibrationType.TEMPERATURE:
                            return ConfigBuilder.CreateApplyTemperatureCalibrationConfig(
                                Path.GetFileName(TEMPERATURE_CALIBRATION_FILE_PATH),
                                measureResultsPaths);

                        default:
                            throw new NotSupportedException();
                    }
                }

                return files.ToArray();
            }
        }
    }
}
