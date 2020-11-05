using Common;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;
using Vectors;
using OxyPlot;
using Utilities;
using Vectors.Extensions;
using DSPLib;
using System.Windows.Media;

namespace Calibrators.ViewModels.Inclinometr
{
    internal enum VectorSet
    {
        ACCELEROMETR,
        MAGNITOMETR,
        ANGLES
    }

    internal class SensorsSetVM
    {
        private SensorsSetVM() { }

        public PlotVM[] Plots { get; private set; }
        public Dictionary<string, NameValuePair<double>[]> ParametersGroups { get; private set; }
        public VectorSet Set { get; private set; }

        public static async Task<SensorsSetVM> CreateAsync(VectorSet set, CalibratedPoint[] points)
        {
            (string XName, 
                string XFullName, 
                string YName, 
                string YFullName, 
                string ZName, 
                string ZFullName, 
                string Units, 
                string YAxisName, 
                Func<CalibratedPoint, (double T, double Angle)>[] Extractors) arguments;

            switch (set)
            {
                case VectorSet.ACCELEROMETR:
                    {
                        var extractors = new Func<CalibratedPoint, (double T, double Angle)>[]
                        {
                            cp => (cp.AccelerometrTemperatures.X, cp.AccelerometrBeforeCal.X),
                            cp => (cp.AccelerometrTemperatures.Y, cp.AccelerometrBeforeCal.Y),
                            cp => (cp.AccelerometrTemperatures.Z, cp.AccelerometrBeforeCal.Z),
                            cp => (cp.AccelerometrTemperatures.X, cp.AccelerometrAfterCal.X),
                            cp => (cp.AccelerometrTemperatures.Y, cp.AccelerometrAfterCal.Y),
                            cp => (cp.AccelerometrTemperatures.Z, cp.AccelerometrAfterCal.Z),
                        };

                        arguments = ("X", "X", "Y", "Y", "Z", "Z", "g", "Значение", extractors);
                    }
                    break;
                case VectorSet.MAGNITOMETR:
                    {
                        var extractors = new Func<CalibratedPoint, (double T, double Angle)>[]
                        {
                            cp => (cp.MagnetometrTemperatures.X, cp.MagnitometrBeforeCal.X),
                            cp => (cp.MagnetometrTemperatures.Y, cp.MagnitometrBeforeCal.Y),
                            cp => (cp.MagnetometrTemperatures.Z, cp.MagnitometrBeforeCal.Z),
                            cp => (cp.MagnetometrTemperatures.X, cp.MagnitometrAfterCal.X),
                            cp => (cp.MagnetometrTemperatures.Y, cp.MagnitometrAfterCal.Y),
                            cp => (cp.MagnetometrTemperatures.Z, cp.MagnitometrAfterCal.Z),
                        };

                        arguments = ("X", "X", "Y", "Y", "Z", "Z", "Гаусс", "Значение", extractors);
                    }
                    break;
                case VectorSet.ANGLES:
                    {
                        var extractors = new Func<CalibratedPoint, (double T, double Angle)>[]
                        {
                            cp => (cp.AvgTemperature, cp.AnglesBeforeCal.X),
                            cp => (cp.AvgTemperature, cp.AnglesBeforeCal.Y),
                            cp => (cp.AvgTemperature, cp.AnglesBeforeCal.Z),
                            cp => (cp.AvgTemperature, cp.AnglesAfterCal.X),
                            cp => (cp.AvgTemperature, cp.AnglesAfterCal.Y),
                            cp => (cp.AvgTemperature, cp.AnglesAfterCal.Z),
                        };

                        arguments = ("Inc", "Inclination", "Azi", "Azimuth", "GTF", "Gravity Toolface Angle", "°", "Угол", extractors);
                    }
                    break;
             
                default:
                    throw new NotSupportedException();
            }

            var curveExtractors = new Func<DataPoint[], CurveVM>[]
            {
                dp => new CurveVM("До калибровки", Colors.Red, dp),
                dp => new CurveVM("До калибровки", Colors.Red, dp),
                dp => new CurveVM("До калибровки", Colors.Red, dp),
                dp => new CurveVM("После калибровки", Colors.Green, dp),
                dp => new CurveVM("После калибровки", Colors.Green, dp),
                dp => new CurveVM("После калибровки", Colors.Green, dp),
            };
            CurveVM[] curves = null;
            var parameters = new double[arguments.Extractors.Length][];
            await Task.Run(() =>
            {
                curves = arguments.Extractors.Length
                        .Range()
                        .ToArray()
                        .AsParallel()
                        .AsOrdered()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                        .Select(calculateCurveWithParameters)
                        .ToArray();

                CurveVM calculateCurveWithParameters(int angleIndex)
                {
                    var angleExtractor = arguments.Extractors[angleIndex];
                    var angles = points
                        .OrderBy(p => p.AvgTemperature)
                        .Select(angleExtractor)
                        .ToArray();
                    var maxDt = DSPFunc.Derivative(angles.Select(a => a.T)).AbsEach().Max();
                    var sampleAngles = MatlabUtils
                        .ColonOp(angles[0].T, maxDt, angles.LastElement().T)
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .Select(t => findColosest(t))
                        .ToArray();
                    var min = sampleAngles.Min(a => a.Angle);

                    Logger.LogInfo(null, $"Взято точек тестирования: {sampleAngles.Length} из: {angles.Length} с шагом по температуре: {maxDt:F2} для ");

                    parameters[angleIndex] = new double[]
                    {
                        DSPFunc.Quantile(sampleAngles.Select(a => a.Angle).SubEach(min).AbsEach(), 0.95)
                    };

                    var dataPoints = angles
                        .Select(a => new DataPoint(a.T, a.Angle))
                        .ToArray();
                    return curveExtractors[angleIndex](dataPoints);

                    (double T, double Angle) findColosest(double t)
                    {
                        (double T, double Angle) prev = (0D, 0D);
                        foreach (var angle in angles)
                        {
                            if (angle.T >= t)
                            {
                                return (angle.T - t).Abs() < (prev.T - t).Abs()
                                    ? angle
                                    : prev;
                            }
                            prev = angle;
                        }

                        throw new Exception();
                    }
                }
            });
            var xVarBefore = parameters[0][0];
            var yVarBefore = parameters[1][0];
            var zVarBefore = parameters[2][0];
            var xVarAfter = parameters[3][0];
            var yVarAfter = parameters[4][0];
            var zVarAfter = parameters[5][0];

            var hasProblem = false;
            var msg = "Обнаружено ухудшение характеристик по параметру {0}";
            if (xVarAfter > xVarBefore)
            {
                Logger.LogWarningEverywhere(msg.Format(arguments.XName));
                hasProblem = true;
            }
            if (yVarAfter > yVarBefore)
            {
                Logger.LogWarningEverywhere(msg.Format(arguments.YName));
                hasProblem = true;
            }
            if (zVarAfter > zVarBefore)
            {
                Logger.LogWarningEverywhere(msg.Format(arguments.ZName));
                hasProblem = true;
            }
            if (hasProblem)
            {
                Logger.LogWarningEverywhere($"Калибровка для позиции Inc={points[0].Angle.X} Azi={points[0].Angle.Y} GTF={points[0].Angle.Z} проведена некачественно");
            }

            return new SensorsSetVM()
            {
                Plots = new PlotVM[]
                {
                    new PlotVM(arguments.XFullName, "Температура", arguments.YAxisName, new [] { curves[0], curves[3] }),
                    new PlotVM(arguments.YFullName, "Температура", arguments.YAxisName, new [] { curves[1], curves[4] }),
                    new PlotVM(arguments.ZFullName, "Температура", arguments.YAxisName, new [] { curves[2], curves[5] })
                },
                ParametersGroups = new Dictionary<string, NameValuePair<double>[]>
                {
                    {
                        "До калибровки",
                        new NameValuePair<double>[]
                        {
                            new NameValuePair<double>($"Вариация по {arguments.XName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = xVarBefore }),
                            new NameValuePair<double>($"Вариация по {arguments.YName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = yVarBefore }),
                            new NameValuePair<double>($"Вариация по {arguments.ZName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = zVarBefore })
                        }
                    },
                    {
                        "После калибровки",
                        new NameValuePair<double>[]
                        {
                            new NameValuePair<double>($"Вариация по {arguments.XName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = xVarAfter }),
                            new NameValuePair<double>($"Вариация по {arguments.YName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = yVarAfter }),
                            new NameValuePair<double>($"Вариация по {arguments.ZName}", arguments.Units, new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = zVarAfter })
                        }
                    }
                },
                Set = set
            };
        }
    }
}
