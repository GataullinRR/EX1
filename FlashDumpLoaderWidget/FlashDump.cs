using Common;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.Models;
using FlashDumpLoaderExports;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;

namespace FlashDumpLoaderWidget
{
    /// <summary>
    /// Flash dump format:
    /// 1) Version [string[?]]
    /// 2) Device Id [byte[1]]
    /// 3) Num of formats [int] <see cref="SectionedDataPacketParser"/>
    /// 4) Row format description [string[?][(3)]]
    /// 5) <see cref="FlashDump.PARSED_DATA_AREA_START_MARKER"/> [byte[see length]]
    /// 6) Parsed data area length [long]
    /// 7) Parsed data area uncompressed length [long]
    /// 8) Parsed data area [byte[(6)]]
    /// 9) Flash dump [byte[till end of file]]
    /// </summary>
    class FlashDump : Disposable
    {
        /// <summary>
        /// Do not use in UI thread!
        /// </summary>
        class WaitTillLengthEnoughStreamProxy : StreamProxyBase
        {
            long _futureLength;
            bool _isNotFinished = true;

            public override long Position
            {
                get => base.Position;
                set
                {
                    if (_isNotFinished)
                    {
                        waitTillLengthReaches(Position + 1);
                    }
                    base.Position = value;
                }
            }

            public override long Length => _futureLength;

            public WaitTillLengthEnoughStreamProxy(long futureLength, Stream baseStream) : base(baseStream)
            {
                if (CanWrite)
                {
                    throw new NotSupportedException("This Proxy does only support read only Streams");
                }

                _futureLength = futureLength;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_isNotFinished)
                {
                    waitTillLengthReaches(offset + count);

                    return base.Read(buffer, offset, count);
                }
                else
                {
                    return base.Read(buffer, offset, count);
                }
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new NotSupportedException("Use sync");
            }

            public override int ReadByte()
            {
                if (_isNotFinished)
                {
                    waitTillLengthReaches(Position + 1);

                    return base.ReadByte();
                }
                else
                {
                    return base.ReadByte();
                }
            }

            void waitTillLengthReaches(long requiredLength)
            {
                while (requiredLength > base.Length && _futureLength >= requiredLength) // Wait till length will be enough to provide data requested
                {
                    Thread.Sleep(10);
                }

                _isNotFinished = Length != _futureLength;
            }
        }

        const K4os.Compression.LZ4.LZ4Level COMPRESSION_LEVEL = K4os.Compression.LZ4.LZ4Level.L00_FAST; // More than enough
        static readonly byte[] PARSED_DATA_AREA_START_MARKER = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        static readonly byte[] DEVICE_DATA_START_MARKER = new byte[] { 0xAB, 0xCD, 0xEF };
        static readonly Version SUPPORTED_VERSION = new Version(2, 0, 0, 0);

        readonly string _path;
        readonly string _uncompressedResultsPath;
        readonly long _flashDumpDataOffset;
        readonly long _parsedDataFutureLength;

        public bool HasParsedData => _uncompressedResultsPath != null;
        public Version Version { get; }
        public IDictionary<RUSDeviceId, EntityDescriptor[]> DataRowDescriptors { get; }

        FlashDump(string path, 
            string uncompressedResultsPath, 
            long flashDumpDataOffset, 
            long parsedDataFutureLength, 
            Version version, 
            IDictionary<RUSDeviceId, EntityDescriptor[]> dataRowDescriptors)
        {
            _path = path;
            _uncompressedResultsPath = uncompressedResultsPath;
            _flashDumpDataOffset = flashDumpDataOffset;
            _parsedDataFutureLength = parsedDataFutureLength;
            Version = version;
            DataRowDescriptors = dataRowDescriptors;
        }

        public static async Task<FlashDump> OpenAsync(string path, AsyncOperationInfo operationInfo)
        {
            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            path = path ?? throw new ArgumentNullException(nameof(path));
            using (var file = File.OpenRead(path))
            using (var reader = new BinaryReader(file, Encoding.UTF8))
            {
                var version = readVersion(reader);
                ensureVersionCorrect(version);
                var deviceId = readDeviceId(reader);
                var formats = readFormats(reader);
                var headerLength = reader.BaseStream.Position;
                var hasParsedData = checkHasParsedData(reader);
                if (hasParsedData)
                {
                    reader.ReadString();
                    var parsedDataAreaLength = reader.ReadInt64();
                    reader.ReadString();
                    var decompressedResultsLength = reader.ReadInt64();
                    reader.ReadString();
                    var unpackedResultsPath = startParsedDataAreaUnpackingAsync(parsedDataAreaLength, reader);

                    return new FlashDump(path, unpackedResultsPath, file.Position, decompressedResultsLength, version, formats);
                }
                else
                {
                    return new FlashDump(path, null, file.Position, 0, version, formats);
                }
            }

            //////////////////////////////////////////////////////////////////////

            Version readVersion(BinaryReader reader)
            {
                reader.ReadString(); // read commentary
                reader.ReadString(); // read commentary
                return new Version(reader.ReadString());
            }

            void ensureVersionCorrect(Version version)
            {
                if (!version.Equals(SUPPORTED_VERSION))
                {
                    throw new NotSupportedException($"Версия {version} не поддерживается");
                }
            }

            RUSDeviceId readDeviceId(BinaryReader reader)
            {
                reader.ReadString();
                return (RUSDeviceId)reader.ReadByte();
            }

            async Task<string> copyParsedDataToPathAsync(Stream section) // Long operation
            {
                var unpackedResultsPath = Storaging.GetTempFilePath();
                using (var result = new FileStream(unpackedResultsPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await section.CopyToAsync(result, 81920, operationInfo);
                }

                return unpackedResultsPath;
            }

            bool checkHasParsedData(BinaryReader reader)
            {
                var has = true;
                reader.ReadString();
                for (int i = 0; i < PARSED_DATA_AREA_START_MARKER.Length; i++)
                {
                    if (PARSED_DATA_AREA_START_MARKER[i] != reader.ReadByte())
                    {
                        has = false;
                    }
                }
                if (!has)
                {
                    reader.BaseStream.Position -= PARSED_DATA_AREA_START_MARKER.Length;
                }

                return has;
            }

            IDictionary<RUSDeviceId, EntityDescriptor[]> readFormats(BinaryReader reader)
            {
                var fs = new Dictionary<RUSDeviceId, EntityDescriptor[]>();

                reader.ReadString();
                var numOfFormatPackets = reader.ReadInt32();
                for (int i = 0; i < numOfFormatPackets; i++)
                {
                    reader.ReadString();
                    var formatDeviceId = EnumUtils.CastSafe<RUSDeviceId>(reader.ReadByte());
                    reader.ReadString();
                    var dataPacketFormatFileString = reader.ReadString();
                    var dataPacketFormatFile = new FileStringSerializer()
                        .Deserialize(dataPacketFormatFileString, DeviceBase.FileType.DATA_PACKET_CONFIGURATION, formatDeviceId);
                    var descriptors = EntitiesDeserializer.ExtractDataPacketDescriptors(dataPacketFormatFile)
                        .ToArray();

                    fs.Add(formatDeviceId, descriptors);
                }

                return fs;
            }

            string startParsedDataAreaUnpackingAsync(long parsedDataAreaLength, BinaryReader reader)
            {
                var unpackedResultsPath = Storaging.GetTempFilePath();
                var unpackedResultsStream = new FileStream(unpackedResultsPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                var dumpFile = File.OpenRead(path); // will be closed when unpacking completes
                dumpFile.Position = reader.BaseStream.Position;
                var packedDataSection = new SectionedStreamProxy(dumpFile, parsedDataAreaLength);
                reader.BaseStream.Position += parsedDataAreaLength;
                operationInfo.CancellationToken.ThrowIfCancellationRequested();
                Task.Run(() =>
                {
                    try
                    {
                        using (dumpFile)
                        using (packedDataSection)
                        using (unpackedResultsStream)
                        using (var dataCompressor = LZ4Stream.Decode(packedDataSection))
                        {
                            dataCompressor.CopyTo(unpackedResultsStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere("Ошибка распаковки", ex);
                    }
                });

                return unpackedResultsPath;
            }
        }

        public static async Task SaveAsync(
            RUSDeviceId deviceId, 
            IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> dataPacketFormat, 
            Stream rawDump, 
            Stream uncompressedParsedData, 
            Stream destination, 
            AsyncOperationInfo operationInfo)
        {
            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            using (var writer = new BinaryWriter(destination, Encoding.UTF8))
            {
                writer.Write("Format contains commentaries (like this one). Do not change them!");

                saveHeader(writer);
                saveParsedData(writer);
                saveRawDump(writer);
            }

            return; /////////////////////////////////////

            void saveHeader(BinaryWriter writer)
            {
                writer.Write("Format version:");
                writer.Write(SUPPORTED_VERSION.ToString());
                writer.Write("This dump was read from device with id:");
                writer.Write((byte)deviceId);
                writer.Write("Num of formats:");
                writer.Write(dataPacketFormat.Count);
                foreach (var kvp in dataPacketFormat)
                {
                    var dataPacketFormatString = new FileStringSerializer().Serialize(kvp.Value);
                    writer.Write("Format for device with id:");
                    writer.Write((byte)kvp.Key);
                    writer.Write("Format description:");
                    writer.Write(dataPacketFormatString);
                }
            }

            void saveParsedData(BinaryWriter writer)
            {
                writer.Write("Parsed data start marker (omit if you want parsing from scratch):");
                if (uncompressedParsedData != null && uncompressedParsedData.Length != 0) // write pasred data
                {
                    for (int i = 0; i < PARSED_DATA_AREA_START_MARKER.Length; i++)
                    {
                        writer.Write(PARSED_DATA_AREA_START_MARKER[i]);
                    }
                    writer.Write("Compressed length (LZ4 algorithm):");
                    writer.Flush();
                    var compressedParsedDataLengthPosition = destination.Position;
                    destination.Position += sizeof(long); // Keep place for the length of compressed data
                    writer.Write("Uncompressed length:");
                    writer.Write(uncompressedParsedData.Length);

                    writer.Write("Parsed data (compressed by LZ4 algorithm):");
                    var oldDestinationPosition = destination.Position;
                    using (var dataCompressor = LZ4Stream.Encode(destination, COMPRESSION_LEVEL, default, true))
                    {
                        uncompressedParsedData.Position = 0;
                        uncompressedParsedData.CopyTo(dataCompressor);
                    }
                    // WTF! Following lines do not provide correct length if moved inside using! See: https://github.com/MiloszKrajewski/K4os.Compression.LZ4/issues/16
                    // uncompressedParsedData.Flush()
                    var compressedDataLength = destination.Position - oldDestinationPosition;
                    destination.Position = compressedParsedDataLengthPosition;
                    writer.Write(compressedDataLength);
                }
            }

            void saveRawDump(BinaryWriter writer)
            {
                writer.Flush();
                writer.BaseStream.Seek(0, SeekOrigin.End);
                writer.Write("Raw dump data:");
                rawDump.Position = 0;
                rawDump.CopyTo(writer.BaseStream);
            }
        }

        public async Task<Stream> OpenRawDataStreamAsync(StreamParameters parameters, AsyncOperationInfo operationInfo)
        {
            throwIfDisposed();

            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, parameters.ReadBufferSize);
            stream.Position = _flashDumpDataOffset;
            return new SectionedStreamProxy(stream, stream.Length - stream.Position);
        }

        public async Task<Stream> OpenParsedDataStreamAsync(StreamParameters parameters, AsyncOperationInfo operationInfo)
        {
            throwIfDisposed();

            if (!HasParsedData)
            {
                throw new NotSupportedException();
            }

            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            Stream stream = new FileStream(_uncompressedResultsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, parameters.ReadBufferSize);
            if (stream.Length != _parsedDataFutureLength)
            {
                stream = new WaitTillLengthEnoughStreamProxy(_parsedDataFutureLength, stream);
            }
            return stream;
        }

        protected override void disposeManagedState()
        {
            try
            {
                File.Delete(_uncompressedResultsPath);
            }
            catch (Exception ex)
            {
                Logger.LogError(null, "Не удалось удалить временные файлы", ex);
            }
        }
    }
}
