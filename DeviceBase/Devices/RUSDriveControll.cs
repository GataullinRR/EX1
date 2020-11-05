using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class RUSDriveControll : RUSFTDIDeviceBase
    {
        public const byte ID = 0b00000110;

        public override RUSDeviceId Id => RUSDeviceId.DRIVE_CONTROL;

        public RUSDriveControll(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {

        }
    }
}
