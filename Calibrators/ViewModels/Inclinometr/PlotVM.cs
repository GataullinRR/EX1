using System;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class PlotVM
    {
        public PlotVM(string name, string xAxisName, string yAxisName, CurveVM[] curves)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            XAxisName = xAxisName ?? throw new ArgumentNullException(nameof(xAxisName));
            YAxisName = yAxisName ?? throw new ArgumentNullException(nameof(yAxisName));
            Curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public string Name { get; }
        public CurveVM[] Curves { get; }
        public string XAxisName { get; }
        public string YAxisName { get; }
    }
}
