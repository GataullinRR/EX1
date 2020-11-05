using Common;
using MVVMUtilities.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;
using Vectors;
using OxyPlot;
using System.Windows.Media;
using Utilities;
using System.Threading;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class AngularCalibrationErrorVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public PlotVM[] Plots { get; private set; }
        public Dictionary<string, NameValuePair<double>[]> ParametersGroups { get; private set; }

        AngularCalibrationErrorVM()
        {

        }

        public static async Task<AngularCalibrationErrorVM> CreateAsync(CalibratedPoint[] points)
        {
            var vm = new AngularCalibrationErrorVM();
            var errors = await getAverageErrors(points).ToArrayAsync();
            vm.Plots = new PlotVM[3];
            vm.Plots[0] = new PlotVM("Средние ошибки по Inc", "Порядковый номер позиции", "Средняя ошибка",
                new CurveVM[2]
                {
                    new CurveVM("До калибровок", Colors.Red, errors.Select((e, i) => new DataPoint(i, e.BeforeCal.X)).ToArray()),
                    new CurveVM("После калибровок", Colors.Green, errors.Select((e, i) => new DataPoint(i, e.AfterCal.X)).ToArray()),
                });
            vm.Plots[1] = new PlotVM("Средние ошибки по Azi", "Порядковый номер позиции", "Средняя ошибка",
                new CurveVM[2]
                {
                    new CurveVM("До калибровок", Colors.Red, errors.Select((e, i) => new DataPoint(i, e.BeforeCal.Y)).ToArray()),
                    new CurveVM("После калибровок", Colors.Green, errors.Select((e, i) => new DataPoint(i, e.AfterCal.Y)).ToArray()),
                });
            vm.Plots[2] = new PlotVM("Средние ошибки по GTF", "Порядковый номер позиции", "Средняя ошибка",
                new CurveVM[2]
                {
                    new CurveVM("До калибровок", Colors.Red, errors.Select((e, i) => new DataPoint(i, e.BeforeCal.Z)).ToArray()),
                    new CurveVM("После калибровок", Colors.Green, errors.Select((e, i) => new DataPoint(i, e.AfterCal.Z)).ToArray()),
                });
            var incAvgErrBefore = vm.Plots[0].Curves[0].Points.Select(p => p.Y).Average();
            var aziAvgErrBefore = vm.Plots[1].Curves[0].Points.Select(p => p.Y).Average();
            var gtfAvgErrBefore = vm.Plots[2].Curves[0].Points.Select(p => p.Y).Average();
            var incAvgErrAfter = vm.Plots[0].Curves[1].Points.Select(p => p.Y).Average();
            var aziAvgErrAfter = vm.Plots[1].Curves[1].Points.Select(p => p.Y).Average();
            var gtfAvgErrAfter = vm.Plots[2].Curves[1].Points.Select(p => p.Y).Average();

            var hasProblem = false;
            var msg = "Обнаружено ухудшение характеристик по углу {0}";
            if (incAvgErrAfter.Abs() > incAvgErrBefore.Abs())
            {
                Logger.LogWarningEverywhere(msg.Format("Inc"));
                hasProblem = true;
            }
            if (aziAvgErrAfter.Abs() > aziAvgErrBefore.Abs())
            {
                Logger.LogWarningEverywhere(msg.Format("Azi"));
                hasProblem = true;
            }
            if (gtfAvgErrAfter.Abs() > gtfAvgErrBefore.Abs())
            {
                Logger.LogWarningEverywhere(msg.Format("GTF"));
                hasProblem = true;
            }
            if (hasProblem)
            {
                Logger.LogWarningEverywhere($"Угловая калибровка проведена некачественно");
            }

            vm.ParametersGroups = new Dictionary<string, NameValuePair<double>[]>
            {
                {
                    "До калибровки",
                    new NameValuePair<double>[]
                    {
                        new NameValuePair<double>("Средняя ошибка по Inc", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = incAvgErrBefore }),
                        new NameValuePair<double>("Средняя ошибка по Azi", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = aziAvgErrBefore }),
                        new NameValuePair<double>("Средняя ошибка по GTF", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = gtfAvgErrBefore })
                    }
                },
                {
                    "После калибровки",
                    new NameValuePair<double>[]
                    {
                        new NameValuePair<double>("Средняя ошибка по Inc", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = incAvgErrAfter }),
                        new NameValuePair<double>("Средняя ошибка по Azi", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = aziAvgErrAfter }),
                        new NameValuePair<double>("Средняя ошибка по GTF", "°", new DoubleValueVM(_ => true, v => v.Round(3)) { ModelValue = gtfAvgErrAfter })
                    }
                }
            };

            return vm;
        }

        static IEnumerable<(V3 BeforeCal, V3 AfterCal)> getAverageErrors(IEnumerable<CalibratedPoint> points)
        {
            foreach (var group in points.GroupBy(p => p.Angle))
            {
                var beforeCal = V3.Zero;
                var afterCal = V3.Zero;
                var count = 0D;
                foreach (var point in group)
                {
                    var dBefore = fix((point.AnglesBeforeCal - point.Angle).Abs);
                    var dAfter = fix((point.AnglesAfterCal - point.Angle).Abs);

                    beforeCal += dBefore;
                    afterCal += dAfter;
                    count++;

                    V3 fix(V3 dAngles)
                    {
                        return new V3(
                            dAngles.X + (dAngles.X > 120 ? -180 : 0),
                            dAngles.Y + (dAngles.Y > 300 ? -360 : 0),
                            dAngles.Z + (dAngles.Z > 300 ? -360 : 0));
                    }
                }
                beforeCal /= count;
                afterCal /= count;

                yield return (beforeCal, afterCal);
            }
        }
    }
}
