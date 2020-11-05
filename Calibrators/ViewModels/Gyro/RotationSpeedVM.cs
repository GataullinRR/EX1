using Calibrators.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities;
using Vectors;
using System.ComponentModel;
using MVVMUtilities.Types;

namespace Calibrators.ViewModels
{
    internal class RotationSpeedVM : INotifyPropertyChanged
    {
        readonly GyroTemperatureCalibrator _calibrator;

        public event PropertyChangedEventHandler PropertyChanged;

        public DoubleValueVM Speed { get; } = new DoubleValueVM(new Interval(40, 80), true, 3);
        public bool IsByNominal
        {
            get => _calibrator.IsRotationSpeedSetByUser;
            set => Speed.IsEditable = _calibrator.IsRotationSpeedSetByUser = value;
        }

        public RotationSpeedVM(GyroTemperatureCalibrator calibrator)
        {
            _calibrator = calibrator;

            Speed.Set = mv => _calibrator.SetRotationSpeed(mv);
            _calibrator.PropertyChanged += _calibrator_PropertyChanged;

            Speed.IsEditable = false;
        }
        void _calibrator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GyroTemperatureCalibrator.ActualRotationSpeed) &&
                Speed.ModelValue != _calibrator.ActualRotationSpeed)
            {
                Speed.ModelValue = _calibrator.ActualRotationSpeed;
            }
        }
    }
}
