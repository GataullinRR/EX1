using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSTelemetry : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000001;

        public override RUSDeviceId Id => RUSDeviceId.TELEMETRY;

        public RUSTelemetry(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
