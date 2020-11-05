using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.ComponentModel;
using MVVMUtilities;
using Vectors;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class DeviceDataVM : INotifyPropertyChanged
    {
        readonly Func<double?> _getCurrentValue;
        readonly Func<double?> _getRequiredValue;
        readonly Action<double> _setRequiredValue;
        readonly Func<Interval?> _getRequiredValueRange;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; }
        public double? CurrentValue => _getCurrentValue();
        public double? RequiredValue
        {
            get => _getRequiredValue();
            set => _setRequiredValue(value.Value);
        }
        public Interval? RequiredValueRange => _getRequiredValueRange();

        public DeviceDataVM(string name, 
            Func<double?> getCurrentValue, 
            Func<double?> getRequiredValue,
            Action<double> setRequiredValue,
            Func<Interval?> getRequiredValueRange,
            INotifyPropertyChanged valueChanged)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _getCurrentValue = getCurrentValue;
            _getRequiredValue = getRequiredValue;
            _setRequiredValue = setRequiredValue;
            _getRequiredValueRange = getRequiredValueRange;

            valueChanged.RedirectAnyChangesTo(
                this, 
                () => PropertyChanged, 
                nameof(CurrentValue), 
                nameof(RequiredValue),
                nameof(RequiredValueRange));
        }
    }
}
