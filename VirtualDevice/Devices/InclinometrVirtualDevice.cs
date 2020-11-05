using System;
using System.Linq;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Models;
using DeviceBase.IOModels;
using System.Collections.Generic;
using DeviceBase;
using DeviceBase.Devices;
using System.IO;

namespace VirtualDevice
{
    class InclinometrVirtualDevice : VirtualRUSDeviceBase
    {
        public override RUSDeviceId Id => RUSDeviceId.INCLINOMETR;

        public InclinometrVirtualDevice(WordSerializator serializator) : base(serializator)
        {
            _serial = 999;

            _calibrationFile = deserialize(Properties.Resources.V01_RUS03_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS03_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = generateDataPacketFormatFile();
            _flashDataPacketFormatFile = _dataPacketFormatFile;
            _temperatureCalibrationFile = deserialize(Properties.Resources.V01_RUS03_TCal, FileType.TEMPERATURE_CALIBRATION);

            byte[] generateDataPacketFormatFile()
            {
                var body = new Enumerable<byte>()
                {
                    "INCL____0101DDMMYYF_".GetASCIIBytes(),
                    serializator.Serialize(_serial),
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // Reserve
                };
                var i = new SmartInt().Add(1);
                addEntity("STAT", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("INC_", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("AZI_", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("GTF_", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("XGpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("YGpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("ZGpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("XMpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("YMpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("ZMpr", (ushort)i, (byte)i.Add(2).DValue, 0, false);
                addEntity("TGad", (ushort)i, (byte)i.Add(2).DValue, 0, true);
                addEntity("TMad", (ushort)i, (byte)i.Add(2).DValue, 0, true);

                return body.ToArray();

                void addEntity(string mnemonicName, ushort position, byte length, byte numOfBits, bool isSigned)
                {
                    body.Add(mnemonicName.GetASCIIBytes());
                    body.Add(_serializator.Serialize(position));
                    body.Add(length);
                    body.Add((isSigned ? (1 << 7) : 0).ToByte().BitOR(numOfBits));
                }
            }
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            var packet = new Enumerable<byte>()
            {
                _serializator.Serialize(_status16 ?? 0),
                _serializator.Serialize(Global.Random.Next(0, 1000).ToUInt16()),
                _serializator.Serialize(Global.Random.Next(0, 1000).ToUInt16()),
                _serializator.Serialize(Global.Random.Next(0, 1000).ToUInt16()),
                Global.Random.NextBytes(12),
                _serializator.Serialize(Global.Random.Next(-200, 1800).ToInt16()),
                _serializator.Serialize(Global.Random.Next(-200, 1800).ToInt16())
            };

            return packet;
        }

        [SalachovCommandHandlerAttribute(Command.CALIBRATION_MODE_SET)]
        public byte[] HandleSetMode(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                var mode = _serializator.Deserialize(fullRequest);
                switch (mode)
                {
                    case 0x0000:
                        _status16 = 0;
                        break;
                    case 0x00FF:
                        _status16 = 1 << 4;
                        break;
                    case 0x0FFF:
                        _status16 = 1 << 5;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                return null;
            }
        }
    }
}
