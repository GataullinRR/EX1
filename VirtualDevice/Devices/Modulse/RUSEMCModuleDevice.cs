using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Devices;
using System.IO;
using DeviceBase;

namespace VirtualDevice
{
    class RUSEMCModuleDevice : VirtualRUSModuleBase
    {
        public override RUSDeviceId Id => RUSDeviceId.EMC_MODULE;

        public RUSEMCModuleDevice(WordSerializator serializator) : base(serializator)
        {
            _serial = 100;
            _status16 = 0;

            _calibrationFile = deserialize(Properties.Resources.V01_RUS40_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS40_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = generateDataPacketFormatFile();

            byte[] generateDataPacketFormatFile()
            {
                var body = new Enumerable<byte>()
                {
                    "EMC_Mdl_0101DDMMYYF_".GetASCIIBytes(),
                    serializator.Serialize(_serial),
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // Reserved
                };
                var i = new SmartInt().Add(1);
                addEntity("STAT", i, i.Add(2).DValue, 0);
                addEntity("STAT", i, i.Add(2).DValue, 0);
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
            return Global.Random.NextBytes(8);
        }

        [SalachovCommandHandlerAttribute(Command.SET_TEST_MODE)]
        public byte[] HandleFrameAccumulationTimeRegisters(byte[] fullRequest, bool isReadRequest)
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
    }
}
