using System;
using System.Collections.Generic;

namespace Calibrators.Models
{
    class Curve
    {
        public string Name { get; }
        public IEnumerable<double> Points { get; }

        public Curve(string name, IEnumerable<double> value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Points = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
