using Common;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Utilities.Extensions;
using OxyPlot;

namespace Calibrators.ViewModels.Inclinometr
{
    /// <summary>
    /// For each position
    /// </summary>
    internal class TemperatureCalibrationErrorVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public OptionVM<SensorsSetVM>[] VectorsSets { get; private set; }
        public OptionVM<SensorsSetVM> SelectedSet { get; set; }
        public string Angle { get; private set; }

        TemperatureCalibrationErrorVM()
        {

        }

        public static async Task<TemperatureCalibrationErrorVM> CreateAsync(CalibratedPoint[] points)
        {
            var vm = new TemperatureCalibrationErrorVM();
            vm.Angle = "Inc={0} Azi={1} GTF={2}".Format(points[0].Angle.X, points[0].Angle.Y, points[0].Angle.Z);
            try
            {
                await calculateParametersAsync();
            }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка расчета контрольных параметров", ex);

                throw;
            }

            return vm;

            async Task calculateParametersAsync()
            {
                vm.VectorsSets = new OptionVM<SensorsSetVM>[]
                {
                   new OptionVM<SensorsSetVM>("Углы", await SensorsSetVM.CreateAsync(VectorSet.ANGLES, points)),
                   new OptionVM<SensorsSetVM>("Магнитометр", await SensorsSetVM.CreateAsync(VectorSet.MAGNITOMETR, points)),
                   new OptionVM<SensorsSetVM>("Акселерометр", await SensorsSetVM.CreateAsync(VectorSet.ACCELEROMETR, points)),
                };
                vm.SelectedSet = vm.VectorsSets[0];
            }
        }
    }
}
