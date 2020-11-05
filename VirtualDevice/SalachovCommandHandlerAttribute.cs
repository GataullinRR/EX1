using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Devices;

namespace VirtualDevice
{
    class SalachovCommandHandlerAttribute : Attribute
    {
        public Command Address { get; }

        public SalachovCommandHandlerAttribute(Command address)
        {
            Address = address;
        }
    }
}
