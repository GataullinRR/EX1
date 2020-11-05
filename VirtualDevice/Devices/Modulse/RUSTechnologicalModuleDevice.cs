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
    class RUSTechnologicalModuleDevice : VirtualRUSModuleBase
    {
        public override RUSDeviceId Id => RUSDeviceId.RUS_TECHNOLOGICAL_MODULE;

        public RUSTechnologicalModuleDevice(WordSerializator serializator) : base(serializator)
        {
            _serial = 100;
            _status32 = Global.Random.NextUInt();

            _calibrationFile = deserialize(Properties.Resources.V01_RUS24_Cal, FileType.CALIBRATION);
            _factorySettingsFile = deserialize(Properties.Resources.V01_RUS24_FSet, FileType.FACTORY_SETTINGS);
            _dataPacketFormatFile = generateDataPacketFormatFile();
            _flashDataPacketFormatFile = generateDataPacketFormatFile();
            _temperatureCalibrationFile = deserialize(Properties.Resources.V01_RUS24_TCal, FileType.TEMPERATURE_CALIBRATION);
            _workModeFile = deserialize(Properties.Resources.V01_RUS24_WMode, FileType.WORK_MODE);

            byte[] generateDataPacketFormatFile()
            {
                var body = new Enumerable<byte>()
                {
                    "TEXMODUL0101DDMMYYF_".GetASCIIBytes(),
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
                        _status32 = 0;
                        break;
                    case 0x00FF:
                        _status32 = 1 << 4;
                        break;
                    case 0x0FFF:
                        _status32 = 1 << 5;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                return null;
            }
        }

        [SalachovCommandHandlerAttribute(Command.SET_DATA_UNLOAD_MODE)]
        public byte[] HandleSetDataUnloadMode(byte[] fullRequest, bool isReadRequest)
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

        [SalachovCommandHandlerAttribute(Command.SET_FLASH_WORK_MODE)]
        public byte[] HandleSetFlashWorkMode(byte[] fullRequest, bool isReadRequest)
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

        [SalachovCommandHandlerAttribute(Command.FLASH_ERASE)]
        public byte[] HandleEraseFlash(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                return new Enumerable<byte>()
                {
                    0, 0, 0xAA, 0xAA
                }.ToArray();
            }
        }

        int _pingRequestCount = 0;
        [SalachovCommandHandlerAttribute(Command.PING)]
        public byte[] HandlePing(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                _pingRequestCount++;
                if (_pingRequestCount % 3 == 0)
                {
                    return new Enumerable<byte>() // Executed
                    {
                        0, 0, 0, 0
                    }.ToArray();
                }
                else
                {
                    return new Enumerable<byte>() // Executing
                    {
                        0, 0, 0xAA, 0xAA
                    }.ToArray();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        byte[] _deviceControlRegisters = Global.Random.NextBytes(8);
        [SalachovCommandHandlerAttribute(Command.DEVICE_CONTROL_REGISTERS)]
        public byte[] HandleDeviceControlRegisters(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _deviceControlRegisters;
            }
            else
            {
                _deviceControlRegisters = fullRequest;

                return null;
            }
        }

        byte[] _accumulationTimeRegisters = Global.Random.NextBytes(8);
        [SalachovCommandHandlerAttribute(Command.FRAME_ACCUMULATION_TIME)]
        public byte[] HandleFrameAccumulationTimeRegisters(byte[] fullRequest, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _accumulationTimeRegisters;
            }
            else
            {
                _accumulationTimeRegisters = fullRequest;

                return null;
            }
        }

        protected override IEnumerable<byte> getDataPacketBytes()
        {
            return Global.Random.NextBytes(8);
        }
    }
}
