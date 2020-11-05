using Common;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyConfig;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class RUSEMCModule : RUSModuleBase
    {
        public const byte ID = 0b00101000;

        public override RUSDeviceId Id => RUSDeviceId.EMC_MODULE;

        public RUSEMCModule(MiddlewaredConnectionInterfaceDecorator pipe) : this(pipe, new IRUSDevice[0])
        {

        }

        public RUSEMCModule(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children) : base(pipe, children)
        {

        }
    }
}
