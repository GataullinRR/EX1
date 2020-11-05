using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSRotationSensor : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000111;

        public override RUSDeviceId Id => RUSDeviceId.ROTATIONS_SENSOR;

        public RUSRotationSensor(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
