using System;
using OxyPlot;
using System.Windows.Media;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class CurveVM
    {
        public CurveVM(string name, Color color, DataPoint[] points)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Color = color;
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }

        public string Name { get; }
        public Color Color { get; }
        public DataPoint[] Points { get; }
    }
}
