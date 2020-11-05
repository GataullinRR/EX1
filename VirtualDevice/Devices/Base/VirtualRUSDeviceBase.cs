using System;
using System.Collections.Generic;
using System.Collections;
using DeviceBase.Models;
using DeviceBase;
using System.Linq;
using Utilities.Extensions;
using Utilities.Types;
using Utilities;
using DeviceBase.Devices;
using System.IO;

namespace VirtualDevice
{
    abstract class VirtualRUSDeviceBase
    {
        protected byte[] _calibrationFile;
        protected byte[] _temperatureCalibrationFile;
        protected byte[] _factorySettingsFile;
        protected byte[] _dataPacketFormatFile;
        protected byte[] _flashDataPacketFormatFile;
        protected byte[] _workModeFile;
        protected ushort? _status16;
        protected uint? _status32;
        protected ushort _serial = 65535;

        public abstract RUSDeviceId Id { get; }
        protected readonly WordSerializator _serializator;

        protected VirtualRUSDeviceBase(WordSerializator serializator)
        {
            _serializator = serializator ?? throw new ArgumentNullException(nameof(serializator));
        }

        protected byte[] deserialize(string file, FileType fileType)
        {
            var fileSerializer = new FileStringSerializer();

            return fileSerializer
                .Deserialize(file, fileType, Id)
                .Select(e => e.RawValue)
                .Flatten()
                .ToArray();
        }

        [SalachovCommandHandlerAttribute(Command.DATA)]
        public byte[] HandleDataPacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return getDataPacketBytes().ToArray();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        protected virtual IEnumerable<byte> getDataPacketBytes()
        {
            return new byte[0];
        }

        [SalachovCommandHandlerAttribute(Command.STATUS)]
        public byte[] HandleGetStatus(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                var body = new Enumerable<byte>();
                if (_status32.HasValue)
                {
                    body = new Enumerable<byte>()
                    {
                        _serializator.Serialize((ushort)(_status32.Value >> 16)),
                        _serializator.Serialize((ushort)(_status32.Value & 0x0000FFFF)),
                    };
                }
                else 
                {
                    body = new Enumerable<byte>()
                    {
                        _serializator.Serialize(_status16 ?? 0),
                    };
                }
                body.Add(_serializator.Serialize(_serial));

                return body.ToArray();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [SalachovCommandHandlerAttribute(Command.TEMPERATURE_CALIBRATION_FILE)]
        public byte[] HandleTemperatureCalibrationFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _temperatureCalibrationFile;
            }
            else
            {
                _temperatureCalibrationFile = requestBody;

                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.CALIBRATION_FILE)]
        public byte[] HandleCalibrationFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _calibrationFile;
            }
            else
            {
                _calibrationFile = requestBody;

                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.FACTORY_SETTINGS_FILE)]
        public byte[] HandleFactorySettingsFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _factorySettingsFile;
            }
            else
            {
                _factorySettingsFile = requestBody;

                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.DATA_PACKET_CONFIGURATION_FILE)]
        public byte[] HandleDataPacketFormatFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _dataPacketFormatFile;
            }
            else
            {
                var headerLength = Files
                    .Descriptors[new FileDescriptorsTarget(FileType.DATA_PACKET_CONFIGURATION, "01", Id)]
                    .Descriptors
                    .Find(d => d.Name == "Резерв").Value.Position;

                _dataPacketFormatFile.Set(requestBody.Take(headerLength), headerLength.Range());

                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.FLASH_DATA_PACKET_CONFIGURATION)]
        public byte[] HandleFlashDataPacketFormatFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _flashDataPacketFormatFile;
            }
            else
            {
                var headerLength = Files
                    .Descriptors[new FileDescriptorsTarget(FileType.FLASH_DATA_PACKET_CONFIGURATION, "01", Id)]
                    .Descriptors
                    .Find(d => d.Name == "Резерв").Value.Position;

                _flashDataPacketFormatFile.Set(requestBody.Take(headerLength), headerLength.Range());

                return null;
            }
        }
        [SalachovCommandHandlerAttribute(Command.WORK_MODE_FILE)]
        public byte[] HandleWorkModeFile(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                return _workModeFile;
            }
            else
            {
                _workModeFile = requestBody;

                return null;
            }
        }

        [SalachovCommandHandlerAttribute(Command.CLOCKS_SYNC)]
        public byte[] HandleClockSync(byte[] requestBody, bool isReadRequest)
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
