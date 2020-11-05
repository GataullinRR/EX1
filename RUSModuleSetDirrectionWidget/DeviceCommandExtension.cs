using DeviceBase.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace RUSModuleSetDirrectionWidget
{
    internal sealed class DeviceCommandExtension : MarkupExtension
    {
        readonly Command _value;

        public DeviceCommandExtension(Command value)
        {
            _value = value;
        }

        public override Object ProvideValue(IServiceProvider sp)
        {
            return _value;
        }
    };
}
