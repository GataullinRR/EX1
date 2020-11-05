using Calibrators.Models;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class ConstantsVM
    {
        readonly CalibrationConstants _constants;

        public DoubleMarshaller BTotal { get; } = new DoubleMarshaller(0, v => v > 0 && v <= 2);
        public DoubleMarshaller DipAngle { get; } = new DoubleMarshaller(0, v => v >= -90 && v <= 90);
    }
}
