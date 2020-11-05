using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSLWDLink : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00001001;

        public override RUSDeviceId Id => RUSDeviceId.LWD_LINK;

        public RUSLWDLink(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
