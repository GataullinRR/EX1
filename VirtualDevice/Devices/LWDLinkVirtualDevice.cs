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

namespace VirtualDevice
{
    class LWDLinkVirtualDevice : VirtualRUSDeviceBase
    {
        public override RUSDeviceId Id => RUSDeviceId.LWD_LINK;

        byte _numOfSectors = 8;
        byte _silentTime = 30;

        public LWDLinkVirtualDevice(WordSerializator serializator) : base(serializator)
        {
            _calibrationFile = deserialize(Properties.Resources.V01_RUS09_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS09_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = generateDataPacketFormatFile();
            _flashDataPacketFormatFile = generateDataPacketFormatFile();

            byte[] generateDataPacketFormatFile()
            {
                var body = new Enumerable<byte>()
                {
                    "SNAPUNIT0101DDMMYYF_".GetASCIIBytes(),
                    serializator.Serialize(_serial),
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // Reserve
                };
                var i = new SmartInt().Add(1);
                addEntity("STAT", i, i.Add(2).DValue, 0);
                addEntity("SECT", i, i.Add(1).DValue, 0);
                addEntity("IDLE", i, i.Add(1).DValue, 0);
                addEntity("TST1", i, i.Add(2).DValue, 0);
                addEntity("TST2", i, i.Add(2).DValue, 0);

                return body.ToArray();

                void addEntity(string mnemonicName, int position, int length, byte numOfBits)
                {
                    body.Add(mnemonicName.GetASCIIBytes());
                    body.Add(_serializator.Serialize((ushort)position));
                    body.Add((byte)length);
                    body.Add(numOfBits);
                }
            }
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            return new Enumerable<byte>()
            {
                _serializator.Serialize(_status16 ?? 0),
                _numOfSectors,
                _silentTime,
                Global.Random.NextBytes(4)
            };
        }

        [SalachovCommandHandlerAttribute(Command.PBP_SETTINGS)]
        public byte[] HandlePBPSettings(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return new[]
                {
                    _numOfSectors,
                    _silentTime
                };
            }
            else
            {
                _numOfSectors = fullRequest[0];
                _silentTime = fullRequest[1];

                return null;
            }
        }
    }
}
