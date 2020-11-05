using MVVMUtilities.Types;
using System;
using System.ComponentModel;

namespace Calibrators.ViewModels.Inclinometr
{
    class NameValuePair<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public object Tag { get; set; }
        public string Name { get; }
        public string Units { get; }
        public ValueVM<T> Value { get; }

        public NameValuePair(string name, string units, ValueVM<T> value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Units = units;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
