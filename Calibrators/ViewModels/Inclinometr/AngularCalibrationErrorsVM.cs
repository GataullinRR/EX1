using Calibrators.Models;
using Common;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using System.Linq;
using System.Collections.Generic;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class AngularCalibrationErrorsVM : CalibrationErrorsVMBase<InclinometrAngularCalibrator>
    {
        public AngularCalibrationErrorVM Error { get; private set; }

        public override PlotVM[] Plots => Error?.Plots;
        public override Dictionary<string, NameValuePair<double>[]> ParametersGroups => Error?.ParametersGroups;

        public AngularCalibrationErrorsVM(InclinometrAngularCalibrator calibrator) : base(calibrator)
        {
            _calibrator.Constants.PropertyChanged += Constants_PropertyChanged;
        }

        async void Constants_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await safeReloadAsync();
        }

        public async override Task ReloadAsync()
        {
            var calibratorApp = new CalibratorApplication(_calibrator.Results, _calibrator.Constants);
            var points = await calibratorApp.CalculateErrorsAsync();

            Error = await AngularCalibrationErrorVM.CreateAsync(points.Flatten().ToArray());
        }

        public override void Clear()
        {
            Error = null;
        }
    }
}
