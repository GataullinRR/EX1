using System;

namespace Calibrators.ViewModels.Inclinometr
{
    class OptionVM<T>
    {
        public OptionVM(string name, T value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public string Name { get; }
        public T Value { get; }
    }
}
