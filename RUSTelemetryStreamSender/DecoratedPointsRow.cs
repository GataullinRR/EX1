using DeviceBase.Models;
using System;

namespace RUSTelemetryStreamSenderExports
{
    public class DecoratedPointsRow : IPointsRow
    {
        public DecoratedPointsRow(double[] points)
            : this(points, RowDecoration.NONE)
        {

        }
        public DecoratedPointsRow(double[] points, RowDecoration decoration)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Decoration = decoration;
        }

        public double[] Points { get; }
        public RowDecoration Decoration { get; }
    }
}