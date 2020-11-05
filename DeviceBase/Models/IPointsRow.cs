using System;

namespace DeviceBase.Models
{
    public interface IPointsRow
    {
        double[] Points { get; }
    }

    public class PointsRow : IPointsRow
    {
        public double[] Points { get; }

        public PointsRow(double[] points)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }
    }
}
