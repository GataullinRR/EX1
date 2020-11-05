using Calibrators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace Calibrators
{
    internal static class Helpers
    {
        public static (double Inc, double Azi, double GTF) GetAngles(
            this InclinometrAngularCalibrator.Position position)
        {
            var p = position
                    .ToString()
                    .Split("_")
                    .Select(v => v.Where(char.IsDigit).Aggregate().ParseToDoubleInvariant())
                    .ToArray();

            return (p[0], p[1], p[2]);
        }
    }
}
