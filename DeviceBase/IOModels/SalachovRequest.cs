using Common;
using CRC16;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class SalachovRequest : RequestBase<SalachovResponse>
    {
        readonly CRC16_CCITT _checksum = new CRC16_CCITT();

        readonly byte[] _header;
        readonly bool _hasLengthField;
        readonly ushort[] _commandWords;
        readonly ResponseLength _responseFullLength;

        public RUSDeviceId DeviceId { get; }
        public Command Address { get; }
        internal bool IsWriteRequest { get; }
        internal bool IsBroadcast => DeviceId == 0;
        public override IEnumerable<byte> Serialized { get; }
        public override RequestTimeout Timeout => RequestTimeout.AS_READ_TIMEOUT;

        SalachovRequest(RUSDeviceId deviceId, Command address, bool isWriteRequest, ResponseLength responseLength, DeviceOperationScope scope)
            : this(deviceId, address, isWriteRequest, responseLength, new IDataEntity[0], scope)
        {

        }
        SalachovRequest(RUSDeviceId deviceId, Command address, bool isWriteRequest, ResponseLength responseLength, IEnumerable<IDataEntity> dataEntities, DeviceOperationScope scope)
        {
            DeviceId = deviceId;
            Address = address;
            IsWriteRequest = isWriteRequest;
            var addressInfo = Address.GetInfo();
            _responseFullLength = responseLength;
            _hasLengthField = _responseFullLength.IsUnknown;

            _commandWords = new ushort[]
            {
                (ushort)(((byte)DeviceId << 9) | ((~0x80) & Address.GetInfo().Address) | (IsWriteRequest ? 1 << 7 : 0)),
                0x0000
            };
            _header = wordsToBytes(_commandWords).ToArray();

            Serialized = serialize(dataEntities);

            IEnumerable<byte> serialize(IEnumerable<IDataEntity> entities)
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

        internal static SalachovRequest CreateReadRequest(RUSDeviceId deviceId, Command address, DeviceOperationScope scope)
        {
            return new SalachovRequest(deviceId, address, false,
                address.GetInfo().ReadResponse.GetInfo().FullResponseLength, new DataEntity[0], scope);
        }
        internal static SalachovRequest CreateWriteRequest(RUSDeviceId deviceId, Command address, IEnumerable<IDataEntity> data, DeviceOperationScope scope)
        {
            return new SalachovRequest(deviceId, address, true,
                address.GetInfo().WriteResponse.GetInfo().FullResponseLength, data, scope);
        }

        protected override async Task<SalachovResponse> deserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo)
        {
            SalachovResponse result = null;
            var answerIterator = new BackedResponseFutureDecorator(inputStream);
            if (DeviceId == RUSDeviceId.ALL) // If broadcast
            {
                Logger.LogInfo(null, "Чтение ответа не требуется, поскольку запрос не предполагает ответа");

                result = new SalachovResponse(this, RequestStatus.OK, ResponseData.NONE, ResponseData.NONE, ResponseData.NONE);
            }
            else
            {
                var header = await answerIterator.WaitAsync(4, WaitMode.EXACT, operationInfo);
                var headerValid = header.SequenceEqual(_header);
                var length = _hasLengthField
                    ? bytesToWord(await answerIterator.WaitAsync(2, WaitMode.EXACT, operationInfo))
                    : 0;
                var isLengthValid = _responseFullLength.IsUnknown
                    ? true
                    : (length + _header.Length + 2 /* CRC */ + (_hasLengthField ? 2 : 0)) == _responseFullLength.Length;
                var answerBody = await answerIterator.WaitAsync(length, WaitMode.EXACT, operationInfo);
                var calculatedChecksum = _checksum.ComputeChecksum(answerIterator.Storage);
                var sentChecksum = bytesToWord(await answerIterator.WaitAsync(2, WaitMode.EXACT, operationInfo));
                var isCRCValid = calculatedChecksum == sentChecksum;

                if (isCRCValid)
                {
                    if (headerValid)
                    {
                        if (isLengthValid)
                        {
                            result = instantiateResponse(RequestStatus.OK);
                        }
                        else
                        {
                            Logger.LogError(null, $"Некорректная длина. Ожидалась: {_responseFullLength.Length.ToString("X2")}, пришла в пакете: {(_hasLengthField ? length.ToString("X2") : "NONE")}");

                            result = instantiateResponse(RequestStatus.WRONG_LENGTH);
                        }
                    }
                    else
                    {
                        Logger.LogError(null, $"Некорректный заголовок. Ожидалась: {_header.ToHex()}, пришла в пакете: {header.ToHex()}");

                        result = instantiateResponse(RequestStatus.WRONG_HEADER);
                    }
                }
                else
                {
                    Logger.LogError(null, $"CRC не совпадает. Ожидалась: {calculatedChecksum.ToString("X2")}, пришла в пакете: {sentChecksum.ToString("X2")}");

                    result = instantiateResponse(RequestStatus.WRONG_CHECKSUM);
                }

                SalachovResponse instantiateResponse(RequestStatus status)
                {
                    return new SalachovResponse(this, status,
                                new InMemoryResponseData(answerIterator.Storage),
                                new InMemoryResponseData(header),
                                new InMemoryResponseData(answerBody));
                }
            }

            return result;
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

        public override SalachovResponse BuildErrorResponse(RequestStatus status)
        {
            return new SalachovResponse(this, status, null, null, null);
        }
    }
}
