using Vectors;

namespace DataViewWidget
{
    class RenderAreaCommand : ChartCommandBase
    {
        public RenderAreaCommand(IntInterval areaRange) : base(ChartCommand.RENDER_AREA)
        {
            AreaRange = areaRange;
        }

        public IntInterval AreaRange { get; }

        public override string ToString()
        {
            return base.ToString() + $" ({AreaRange.ToString("L", null)})";
        }
    }
}
