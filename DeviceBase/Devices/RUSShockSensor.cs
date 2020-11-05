using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSShockSensor : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000101;

        public override RUSDeviceId Id => RUSDeviceId.SHOCK_SENSOR;

        public RUSShockSensor(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
