using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;
using Vectors;

namespace Calibrators.Models.Inclinometr
{
    static class IncMath
    {
        public static IEnumerable<(double Inc, double Azi, double Gtf)> GetAngles(IEnumerable<(V3 G, V3 H)> vectors)
        {
            const double PI = System.Math.PI;
            Func<double, double, double> pow = Math.Pow;
            Func<double, double, double> atan2 = Math.Atan2;

            var iterator = vectors.GetEnumerator();
            while (iterator.MoveNext())
            {
                var vector = iterator.Current;

                var sI = pow(pow(vector.G.X, 2.0) + pow(vector.G.Y, 2.0), 0.5);
                var cI = -vector.G.Z;
                var GT = pow(pow(sI, 2.0) + pow(cI, 2.0), 0.5);

                var INC = atan2(sI, cI);
                var GTF = atan2(vector.G.Y, vector.G.X);
                if (GTF < 0)
                {
                    GTF += 2 * PI;
                }
                var AZI = atan2(
                    -(vector.H.X * vector.G.Y + vector.H.Y * vector.G.X) * GT,
                    (vector.H.X * vector.G.X - vector.H.Y * vector.G.Y) * cI + vector.H.Z * pow(sI, 2.0));
                if (AZI < 0)
                {
                    AZI += 2 * PI;
                }
                var angles = (INC * 180 / PI, AZI * 180 / PI, GTF * 180 / PI);

                yield return angles;
            }
        }
    }
}
