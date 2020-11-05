using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Devices;

namespace VirtualDevice.Devices
{
    class RUSModule : VirtualRUSModuleBase
    {
        public override RUSDeviceId Id => RUSDeviceId.RUS_MODULE;

        public RUSModule(WordSerializator serializator) : base(serializator)
        {
            _serial = 100;
            _status32 = 0;
        }

        [SalachovCommandHandlerAttribute(Command.REG_DATA_FRAME)]
        public byte[] HandleRegDataFramePacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return Global.Random.NextBytes((requestBody.Length - 10) * 4);
            }
        }

        [SalachovCommandHandlerAttribute(Command.DRILL_DIRECTLY)]
        public byte[] HandleDrillDirectlyPacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.KEEP_MTF)]
        public byte[] HandleKeepMTFPacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.ROTATE_WITH_CONSTANT_SPEED)]
        public byte[] HandleRotateWithConstantSpeedPacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.TURN_ON_AZIMUTH)]
        public byte[] HandleTurnOnAzimuthPacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return null;
            }
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            return Global.Random.NextBytes(6);
        }
    }
}
