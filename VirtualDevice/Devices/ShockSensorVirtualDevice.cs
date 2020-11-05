using DeviceBase;
using DeviceBase.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace VirtualDevice.Devices
{
    class ShockSensorVirtualDevice : VirtualRUSDeviceBase
    {
        public override RUSDeviceId Id => RUSDeviceId.SHOCK_SENSOR;

        public ShockSensorVirtualDevice(WordSerializator serializator) : base(serializator)
        {
            _calibrationFile = deserialize(Properties.Resources.V01_RUS05_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS05_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = deserialize(Properties.Resources.V01_RUS05_DPConf, FileType.DATA_PACKET_CONFIGURATION);
            _flashDataPacketFormatFile = deserialize(Properties.Resources.V01_RUS05_DPConf, FileType.DATA_PACKET_CONFIGURATION);
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            var packet = new Enumerable<byte>()
            {
                Global.Random.NextBytes(20),
                _serializator.Serialize(Global.Random.Next(480, 550).ToUInt16()),
                _serializator.Serialize(Global.Random.Next(480, 550).ToUInt16()),
                _serializator.Serialize(Global.Random.Next(480, 550).ToUInt16())
            };

            return packet;
        }
    }
}
