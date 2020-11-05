using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBase.Devices
{
    class RUSModule : RUSModuleBase
    {
        public const byte ID = 0b00000010;

        public override RUSDeviceId Id => RUSDeviceId.RUS_MODULE;

        public RUSModule(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children) : base(pipe, children)
        {

        }
    }
}
