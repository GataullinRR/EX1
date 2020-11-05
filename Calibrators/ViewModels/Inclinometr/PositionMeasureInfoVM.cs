using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using Calibrators.Models;
using WPFUtilities.Converters;
using System.Windows.Media.Imaging;
using WPFUtilities.Extensions;
using System.ComponentModel;

namespace Calibrators.ViewModels.Inclinometr
{
    public class BoolToIconConverter : SmartConverterTemplate<bool, BitmapImage>
    {
        readonly BitmapImage _ok = Properties.Resources.Do_s.ToBitmapImage();
        readonly BitmapImage _notOk = Properties.Resources.Dont_s.ToBitmapImage();

        public override BitmapImage Convert(bool value)
        {
            return value ? _ok : _notOk;
        }
    }

    internal class PositionMeasureInfoVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsMeasured { get; set; }
        public InclinometrAngularCalibrator.PrecisePosition Position { get; }

        public PositionMeasureInfoVM(InclinometrAngularCalibrator.PrecisePosition position)
        {
            Position = position ?? throw new ArgumentNullException(nameof(position));
        }

        public override string ToString()
        {
            return Position.ToString();
        }
    }
}
