using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSIzmeritel : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000100;

        public override RUSDeviceId Id => RUSDeviceId.IZMERITEL;

        public RUSIzmeritel(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
