using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.Windows.Controls;
using WPFUtilities.Types;
using System.ComponentModel;
using Common;
using DeviceBase.Devices;

namespace Calibrators
{
    public interface ICalibratorModel : INotifyPropertyChanged
    {
        bool HasCalibrationBegun { get; }
        RUSDeviceId TargetDeviceId { get; }
        string CalibrationName { get; }
        IDataProvider DataProvider { get; }
        ActionCommand Begin { get; }
        ActionCommand Discard { get; }
    }

    public interface ICalibrator
    {
        IEnumerable<IWidget> Widgets { get; }
        ICalibratorModel Model { get; }
    }

    class Calibrator : ICalibrator
    {
        public IEnumerable<IWidget> Widgets { get; }
        public ICalibratorModel Model { get; }

        public Calibrator(ICalibratorModel model, params IWidget[] widgets)
        {
            Model = model;
            Widgets = widgets;
        }
    }
}
