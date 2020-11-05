using Calibrators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Utilities;

namespace Calibrators.Views
{
    internal sealed class ShockTestModeExtension : MarkupExtension
    {
        readonly object _testMode;

        public ShockTestModeExtension(string testMode)
        {
            _testMode = testMode.StartsWith("SHOCK-") 
                ? (object)EnumUtils.Parse<AxisShockTestMode>(testMode.Replace("SHOCK-", ""))
                : (object)EnumUtils.Parse<AxisTestMode>(testMode);
        }

        public override Object ProvideValue(IServiceProvider sp)
        {
            return _testMode;
        }
    };
}
