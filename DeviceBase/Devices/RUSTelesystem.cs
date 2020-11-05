using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSTelesystem : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00001000;

        public override RUSDeviceId Id => RUSDeviceId.TELESYSTEM;

        public RUSTelesystem(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
