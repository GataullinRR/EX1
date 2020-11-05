using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSInclinometr : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000011;

        public override RUSDeviceId Id => RUSDeviceId.INCLINOMETR;

        public RUSInclinometr(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
