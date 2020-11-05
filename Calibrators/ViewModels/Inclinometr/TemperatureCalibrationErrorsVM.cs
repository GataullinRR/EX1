using Calibrators.Models;
using MVVMUtilities.Types;
using System.Threading.Tasks;
using Vectors;
using static Calibrators.Models.InclinometrTemperatureCalibrator;
using Utilities.Extensions;
using System.Collections.Generic;
using System.Linq;
using Utilities.Types;
using Utilities;

namespace Calibrators.ViewModels.Inclinometr
{
    using ErrorsCollection = EnhancedObservableCollection<TemperatureCalibrationErrorVM>;

    internal class TemperatureCalibrationErrorsVM : CalibrationErrorsVMBase<InclinometrTemperatureCalibrator>
    {
        readonly Dictionary<bool, ErrorsCollection> _errors = new Dictionary<bool, ErrorsCollection>();
        public ErrorsCollection Errors { get; private set; } = new ErrorsCollection();
        public TemperatureCalibrationErrorVM Error { get; set; }
        public bool UseTestPoint { get; set; } = true;
        public bool HasTestPoint { get; private set; }

        public override PlotVM[] Plots => Error?.SelectedSet?.Value?.Plots;
        public override Dictionary<string, NameValuePair<double>[]> ParametersGroups => Error?.SelectedSet?.Value?.ParametersGroups;

        public TemperatureCalibrationErrorsVM(InclinometrTemperatureCalibrator calibrator) : base(calibrator)
        {
            PropertyChanged += TemperatureCalibrationErrorsVM_PropertyChanged;
            _calibrator.PropertyChanged += _calibrator_PropertyChanged;
        }

        void _calibrator_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            HasTestPoint = _calibrator.Results.Any(r => r.Mode.GetAttribute<TestAngleAttribute>() != null);
        }

        async void TemperatureCalibrationErrorsVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UseTestPoint))
            {
                Update();
            }
            if (e.PropertyName == nameof(HasTestPoint))
            {
                await safeReloadAsync();
            }
        }

        void Update()
        {
            var errorAngle = Error?.Angle;
            var vector = Error?.SelectedSet?.Name;
            Errors = _errors[UseTestPoint];
            Error = Errors.FirstOrDefault(err => err.Angle == errorAngle) ?? Errors.FirstItem();
            Error.SelectedSet = Error.VectorsSets.FirstOrDefault(s => s.Name == vector) ?? Error.VectorsSets.FirstItem();
        }

        public override async Task ReloadAsync()
        {
            using (new FlagInverseAction(true, v => IsLoading = v))
            {
                var hasOldState = Error != null;
                var oldAngle = Error?.Angle;
                var oldVSet = Error?.SelectedSet?.Value?.Set;

                foreach (var useTestAngle in new bool[] { false, true })
                {
                    var baseMeasures = _calibrator.Results.ToList();
                    var calibratorApp = new CalibratorApplication(baseMeasures, _calibrator.Constants);
                    var points = await calibratorApp.CalculateErrorsAsync(useTestAngle);

                    var errors = new ErrorsCollection();
                    var tasks = points.Length.Range()
                        .Select(i => TemperatureCalibrationErrorVM.CreateAsync(points[i]))
                        .ToArray();
                    for (int i = 0; i < points.Length; i++)
                    {
                        errors.Add(await tasks[i]);
                        errors[i].PropertyChanged += TemperatureCalibrationErrorsVM_PropertyChanged1;
                    }

                    _errors[useTestAngle] = errors;
                }

                Update();

                if (hasOldState)
                {
                    Error = _errors[UseTestPoint].First(e => e.Angle == oldAngle);
                    Error.SelectedSet = Error.VectorsSets.First(s => s.Value.Set == oldVSet);
                }
            }
        }

        protected override bool isEnabled()
        {
            return _calibrator.Results
                .Select(r => r.Mode)
                .ContainsAll(EnumUtils
                    .GetValues<IncTCalAngle>()
                    .Where(a => a.GetAttribute<TestAngleAttribute>() == null));
        }

        bool _updating = false;
        private void TemperatureCalibrationErrorsVM_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TemperatureCalibrationErrorVM.SelectedSet) && !_updating)
            {
                using (new FlagInverseAction(true, v => _updating = v))
                {
                    var error = (TemperatureCalibrationErrorVM)sender;
                    foreach (var others in _errors.Values.Flatten().Where(er => er != error))
                    {
                        others.SelectedSet = others.VectorsSets.FirstOrDefault(s => s.Value.Set == error.SelectedSet.Value.Set) 
                            ?? others.SelectedSet;
                    }
                }
            }
        }

        public override void Clear()
        {
            Error = null;
            Errors.Clear();
            _errors.Clear();
        }
    }
}