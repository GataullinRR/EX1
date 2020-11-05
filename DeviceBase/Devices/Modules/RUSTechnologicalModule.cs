using Common;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TinyConfig;
using Utilities.Extensions;
using Utilities.Types;
using Utilities;
using DeviceBase.Helpers;
using DeviceBase.IOModels.Protocols;

namespace DeviceBase.Devices
{
    class RUSTechnologicalModule : RUSModuleBase
    {
        public const byte ID = 0b00011000;

        public override RUSDeviceId Id => RUSDeviceId.RUS_TECHNOLOGICAL_MODULE;

        public RUSTechnologicalModule(MiddlewaredConnectionInterfaceDecorator pipe) : this(pipe, new IRUSDevice[0])
        {

        }
        public RUSTechnologicalModule(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children) 
            : base(pipe, children)
        {

        }
    }
}
