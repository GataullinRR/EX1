using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using Vectors;
using System.ComponentModel;
using Calibrators.Models;

namespace Calibrators.ViewModels
{
    internal class ProgressVM : IRichProgress
    {
        public double Progress { get; private set; }
        public double ProgressInPercents => Progress / MaxProgress;
        public double MaxProgress { get; set; } = 1D;
        public ProgressState State { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Report(double value)
        {
            Progress = value;
        }
    }
}
