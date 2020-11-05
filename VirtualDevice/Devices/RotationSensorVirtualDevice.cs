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
    class RotationSensorVirtualDevice : VirtualRUSDeviceBase
    {
        public override RUSDeviceId Id => RUSDeviceId.ROTATIONS_SENSOR;

        public RotationSensorVirtualDevice(WordSerializator serializator) : base(serializator)
        {
            _calibrationFile = deserialize(Properties.Resources.V01_RUS07_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS07_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = generateDataPacketFormatFile();
            _flashDataPacketFormatFile = generateDataPacketFormatFile();
            _temperatureCalibrationFile = deserialize(Properties.Resources.V01_RUS07_TCal, FileType.TEMPERATURE_CALIBRATION);

            byte[] generateDataPacketFormatFile()
            {
                var body = new Enumerable<byte>()
                {
                    "Bl_Gyro_0101DDMMYYF_".GetASCIIBytes(),
                    serializator.Serialize(_serial),
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // Reserve
                };
                var i = new SmartInt().Add(1);
                addEntity("STAT", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("SGY1", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("SRO1", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("ANG1", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("TEM1", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("SGY2", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("SRO2", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("ANG2", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("TEM2", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("ADSG", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("ADH1", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("ADH2", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("ADTM", (ushort)i, (byte)i.Add(2).DValue, 0, true);

                return body.ToArray();

                void addEntity(string mnemonicName, ushort position, byte length, byte numOfBits, bool isSigned)
                {
                    body.Add(mnemonicName.GetASCIIBytes());
                    body.Add(_serializator.Serialize(position));
                    body.Add(length);
                    body.Add((isSigned ? 1 : 0).ToByte().BitLShift(7).BitOR(numOfBits));
                }
            }
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            var packet = new Enumerable<byte>()
            {
                _serializator.Serialize(_status16 ?? 0),
                Global.Random.NextBytes(24)
            };

            return packet;
        }
    }
}
