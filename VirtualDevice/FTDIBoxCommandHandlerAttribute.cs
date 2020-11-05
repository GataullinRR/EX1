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
    class FTDIBoxCommandHandlerAttribute : Attribute
    {
        public byte[] RequestBytes { get; }
        public int? FullRequestLength { get; }
        public bool HasLenghtField => !FullRequestLength.HasValue;

        public FTDIBoxCommandHandlerAttribute(byte[] commandBytes, int requestLength)
            : this(commandBytes, (int?)requestLength)
        {

        }
        public FTDIBoxCommandHandlerAttribute(byte[] commandBytes)
            : this(commandBytes, null)
        {

        }

        FTDIBoxCommandHandlerAttribute(byte[] commandBytes, int? requestLength)
        {
            RequestBytes = commandBytes;
            FullRequestLength = requestLength;
        }
    }
}
