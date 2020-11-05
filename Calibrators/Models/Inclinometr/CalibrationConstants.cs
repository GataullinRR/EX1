using Utilities;
using System.ComponentModel;
using Utilities.Extensions;

namespace Calibrators.Models
{
    class CalibrationConstants : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double BTotal { get; set; }
        public double DipAngle { get; set; }

        public string GenerateFile()
        {
            return $"{DipAngle.ToStringInvariant()}{Global.NL}{BTotal.ToStringInvariant()}";
        }
    }
}
