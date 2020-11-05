using Calibrators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Utilities;

namespace Calibrators.Views
{
    internal sealed class GyroTemperatureCalibrationModeExtension : MarkupExtension
    {
        readonly GyroTemperatureCalibrator.Modes _value;

        public GyroTemperatureCalibrationModeExtension(GyroTemperatureCalibrator.Modes value)
        {
            _value = value;
        }

        public override Object ProvideValue(IServiceProvider sp)
        {
            return _value;
        }
    };
}
