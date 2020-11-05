using Common;
using CRC16;
using DeviceBase.Attributes;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public enum ResponseReadMode
    {
        STANDARD_PROTOCOL,
        TILL_TIMEOUT,
        CONSTRAINED,
    }

    public class ResponseInfo
    {
        public ResponseReadMode ReadMode { get; }
        public ResponseLength ResponseFullLength { get; }
        public IEnumerable<byte> ExpectedAnswer { get; }

        public ResponseInfo(ResponseReadMode readMode, ResponseLength responseFullLength, IEnumerable<byte> expectedAnswer)
        {
            ReadMode = readMode;
            ResponseFullLength = responseFullLength ?? throw new ArgumentNullException(nameof(responseFullLength));
            ExpectedAnswer = expectedAnswer;
        }
    }

    public interface IRequest
    {
        byte DeviceId { get; }
        RequestAddress Address { get; }
        IEnumerable<byte> Serialized { get; }
        Task<BasicResponse> DeserializeResponseAsync(IEnumerable<byte> inputStream, AsyncOperationInfo operationInfo);
    }

    abstract class RequestProxyBase : IRequest
    {
        protected readonly IRequest _base;

        public byte DeviceId => _base.DeviceId;
        public RequestAddress Address => _base.Address;
        public virtual IEnumerable<byte> Serialized => _base.Serialized;

        public RequestProxyBase(IRequest @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual Task<BasicResponse> DeserializeResponseAsync(IEnumerable<byte> inputStream, AsyncOperationInfo operationInfo)
        {
            return _base.DeserializeResponseAsync(inputStream, operationInfo);
        }
    }

#warning Too many fields, use interface and 2 separate instances
#warning Should not know about protocol!
    public class MultiProtocolRequest : IRequest
    {
        readonly CRC16_CCITT _checksum = new CRC16_CCITT();

        readonly byte[] _header;
        readonly bool _hasLengthField;
        readonly ushort[] _commandWords;
        readonly ResponseInfo _responseInfo;

        public byte DeviceId { get; }
        public RequestAddress Address { get; }
        internal bool IsWriteRequest { get; }
        public IEnumerable<byte> Serialized { get; private set; }

        MultiProtocolRequest(ResponseInfo responseInfo)
            : this(default, RequestAddress.NONE, default, responseInfo, new IDataEntity[0])
        {

        }
        MultiProtocolRequest(byte deviceId, RequestAddress address, bool isWriteRequest, ResponseInfo responseInfo)
            : this(deviceId, address, isWriteRequest, responseInfo, new IDataEntity[0])
        {

        }
        MultiProtocolRequest(byte deviceId, RequestAddress address, bool isWriteRequest, ResponseInfo responseInfo, IEnumerable<IDataEntity> dataEntities)
        {
            DeviceId = deviceId;
            Address = address;
            IsWriteRequest = isWriteRequest;
            var addressInfo = Address.GetInfo();
            _responseInfo = responseInfo;
            _hasLengthField = _responseInfo.ResponseFullLength.IsUnknown;

            if (address != RequestAddress.NONE)
            {
                _commandWords = new ushort[]
                {
                    (ushort)((DeviceId << 9) | ((~0x80) & Address.GetInfo().Address) | (IsWriteRequest ? 1 << 7 : 0)),
                    0x0000
                };
                _header = wordsToBytes(_commandWords).ToArray();
            }

            Serialized = serialize(dataEntities);

            IEnumerable<byte> serialize(IEnumerable<IDataEntity> entities)
            {
                if (address == RequestAddress.NONE)
                {
                    return new byte[0];
                }
                else
                {
                    var dataBytes = entities
                        .Select(e => e.RawValue)
                        .Flatten()
                        .ToArray();
                    var zeroTrailShouldBeAdded = dataBytes.Length % 2 == 1;
                    var actualLength = zeroTrailShouldBeAdded
                        ? dataBytes.Length + 1
                        : dataBytes.Length;
                    var requestBytes = new Enumerable<byte>()
                    {
                        _header,
                        IsWriteRequest // If there is data - we need to add length of the data
                            ? wordsToBytes(((ushort)actualLength).ToSequence())
                            : new byte[0],
                        dataBytes,
                        zeroTrailShouldBeAdded
                            ? new byte[1]
                            : new byte[0]
                    };

                    requestBytes.Add(wordsToBytes(_checksum.ComputeChecksum(requestBytes).ToSequence()));

                    return requestBytes;
                }
            }
        }

        internal static MultiProtocolRequest CreateReadRequest(byte deviceId, RequestAddress address)
        {
            return new MultiProtocolRequest(deviceId, address, false,
                new ResponseInfo(ResponseReadMode.STANDARD_PROTOCOL, address.GetInfo().ReadResponse.GetInfo().FullResponseLength, null), new DataEntity[0]);
        }
        internal static MultiProtocolRequest CreateWriteRequest(byte deviceId, RequestAddress address, IEnumerable<IDataEntity> data)
        {
            return new MultiProtocolRequest(deviceId, address, true,
                new ResponseInfo(ResponseReadMode.STANDARD_PROTOCOL, address.GetInfo().WriteResponse.GetInfo().FullResponseLength, null), data);
        }
        internal static MultiProtocolRequest CreateRequest(IEnumerable<byte> data, ResponseInfo responseInfo)
        {
            return new MultiProtocolRequest(default, RequestAddress.NONE, default, responseInfo)
            {
                Serialized = data
            };
        }

#warning add errorHandling
        public async Task<BasicResponse> DeserializeResponseAsync(IEnumerable<byte> inputStream, AsyncOperationInfo operationInfo)
        {
            var sw = Stopwatch.StartNew();
            IEnumerable<byte> dataRead = null;
            Func<long> getDataReadCount = () => 0;
            BasicResponse result = null;
            using (Logger.Indent)
            {
                Logger.LogInfo(null, "Чтение ответа...");

                try
                {
                    if (Address == RequestAddress.NONE)
                    {
                        await deserializeCustomResponseAsync();
                    }
                    else
                    {
                        await deserializeStandardResponseAsync();
                    }
                }
                catch (Exception ex)
                {
                    logError(ex);

                    throw;
                }

                logResult();

                return result;

                void logResult()
                {
                    if (result.Status == RequestStatus.OK)
                    {
                        Logger.LogOK(null, $"Запрос успешно выполнен");
                    }
                    else
                    {
                        Logger.LogErrorEverywhere($"Не удалось выполнить запрос-NL{result.Status}");
                    }
                }

                async Task deserializeCustomResponseAsync()
                {
                    Stream stream = new MemoryStream();
                    if (_responseInfo.ResponseFullLength.IsUnknown ||
                        _responseInfo.ResponseFullLength.Length > 10 * 1024 * 1024)
                    {
                        var path = Path.GetTempFileName();
                        stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

                        Logger.LogInfo(null, $"Создан буфер чтения по пути {path}");
                    }
                    dataRead = stream.AsEnumerable();
                    getDataReadCount = () => stream.Length;

                    var iterator = inputStream.GetEnumerator(); // Will generate timeout exception if there is no data

                    switch (_responseInfo.ReadMode)
                    {
                        case ResponseReadMode.TILL_TIMEOUT:
                            try
                            {
                                while (true)
                                {
                                    iterator.MoveNext();
                                    stream.WriteByte(iterator.Current);
                                    operationInfo.Progress.Report(1);
                                }
                            }
                            catch (TimeoutException) { }
                            break;
                        case ResponseReadMode.CONSTRAINED:
                            var max = _responseInfo.ResponseFullLength.IsUnknown ? long.MaxValue : _responseInfo.ResponseFullLength.Length;
                            for (long i = 0; i < max && iterator.MoveNext(); i++)
                            {
                                stream.WriteByte(iterator.Current);
                                operationInfo.Progress.Report(1);
                            }
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    logDataRead();

                    if (!_responseInfo.ResponseFullLength.IsUnknown &&
                        _responseInfo.ExpectedAnswer != null &&
                        !stream.AsEnumerable().Take((int)_responseInfo.ResponseFullLength.Length).SequenceEqual(_responseInfo.ExpectedAnswer))
                    {
                        result = new BasicResponse(this, RequestStatus.NOT_EXPECTED_RESPONSE_BYTES, dataRead);
                    }
                    else
                    {
                        await stream.FlushAsync(operationInfo.CancellationToken);

                        result = new BasicResponse(this, RequestStatus.OK, dataRead);
                    }
                }

                async Task deserializeStandardResponseAsync() // Salahov's protocol
                {
                    var fullAnswer = new List<byte>();
                    dataRead = fullAnswer;
                    getDataReadCount = () => fullAnswer.Count;
                    var answerIterator = inputStream
                        .StartEnumeration()
                        .UseStorage(fullAnswer);
                    if (DeviceId == 0) // If broadcast
                    {
                        Logger.LogInfo(null, "Чтение ответа не требуется, поскольку запрос не предполагает ответа");

                        result = new SalachovResponse(this, RequestStatus.OK, new byte[0], null);
                    }
                    else
                    {
                        var headerValid = answerIterator
                            .Pull(_header.Length)
                            .SequenceEqual(_header);
                        var length = _hasLengthField
                            ? bytesToWord(answerIterator.Pull(2))
                            : 0;
                        var isLengthValid = _responseInfo.ResponseFullLength.IsUnknown
                            ? true
                            : (length + _header.Length + 2 /* CRC */ + (_hasLengthField ? 2 : 0)) == _responseInfo.ResponseFullLength.Length;
                        var answerBody = answerIterator.Pull(length);
                        var calculatedChecksum = _checksum.ComputeChecksum(fullAnswer);
                        var sentChecksum = bytesToWord(answerIterator.Pull(2));
                        var isCRCValid = calculatedChecksum == sentChecksum;

                        logDataRead();

                        var sections = new Dictionary<string, IEnumerable<byte>>();
                        sections[SalachovProtocol.BODY_SECTION_KEY] = answerBody;
                        if (isCRCValid)
                        {
                            if (headerValid)
                            {
                                if (isLengthValid)
                                {
                                    result = new SalachovResponse(this, RequestStatus.OK, fullAnswer, sections);
                                }
                                else
                                {
                                    result = new SalachovResponse(this, RequestStatus.WRONG_LENGTH, fullAnswer, sections);
                                }
                            }
                            else
                            {
                                result = new SalachovResponse(this, RequestStatus.WRONG_HEADER, fullAnswer, sections);
                            }
                        }
                        else
                        {
                            result = new SalachovResponse(this, RequestStatus.WRONG_CHECKSUM, fullAnswer, sections);
                        }
                    }
                }

                void logDataRead()
                {
                    Logger.LogInfo(null, $"Пакет ответа был успешно прочитан{Global.NL}Полная длина: {getDataReadCount()}, длительность чтения: {sw.Elapsed.TotalMilliseconds.ToString("F2")} мс{getBufferRepresentation()}");
                }

                void logError(Exception ex)
                {
                    Logger.LogError(null, $"Ошибка во время чтения/десериализации пакета. Было прочитано: {getDataReadCount()}, длительность чтения: {sw.Elapsed.TotalMilliseconds.ToString("F2")} мс{getBufferRepresentation()}", ex);
                }

                string getBufferRepresentation()
                {
                    const int MAX_AMOUNT_OF_BYTES_INSIDE_LOG = 1000;
                    var tooManyData = getDataReadCount() > MAX_AMOUNT_OF_BYTES_INSIDE_LOG;
                    var data = dataRead;
                    if (tooManyData)
                    {
                        data = data.Take(MAX_AMOUNT_OF_BYTES_INSIDE_LOG);
                    }

                    return $"{Global.NL}Первые {MAX_AMOUNT_OF_BYTES_INSIDE_LOG} байт из {getDataReadCount()}".IfOrDefault(tooManyData) +
                           $"{Global.NL}Данные<HEX>:{data.Select(b => b.ToString("X2").PadLeft(3)).Aggregate(" ")}" +
                           $"{Global.NL}Данные<DEC>:{data.Select(b => b.ToString("D3").PadLeft(3)).Aggregate(" ")}";
                }
            }
        }

        IEnumerable<byte> wordsToBytes(IEnumerable<ushort> words)
        {
            foreach (var word in words)
            {
                var bytes = BitConverter.GetBytes(word).Reverse();
                foreach (var b in bytes)
                {
                    yield return b;
                }
            }
        }
        ushort bytesToWord(IEnumerable<byte> wordBytes)
        {
            return BitConverter.ToUInt16(wordBytes.Take(2).Reverse().ToArray(), 0);
        }
    }
}
