using Common;
using CRC16;
using DeviceBase.Attributes;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    enum ResponseReadMode
    {
        /// <summary>
        /// The data will be read till timeout (timeout error wont be generated)
        /// </summary>
        TILL_TIMEOUT,
        READ_BY_TARGET,
        CONSTRAINED_BY_ANSWER,
    }

    enum FTDIBoxRequestAddress
    {
        HS_SET_LOW_SPEED_MODE,
        LS_SET_HIGH_SPEED_MODE,
        HS_ACTIVATE_CHANNEL4,
        LS_ACTIVATE_CHANNEL4,
        HS_GET_FLASH_LENGTH,
        HS_READ_ALL_FLASH,
        LS_DEVICE_REQUEST
    }

    /// <summary>
    /// Create base class
    /// </summary>
    class FTDIBoxRequest : RequestBase<IResponse>
    {
        class ResponseInfo
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

        /// <summary>
        /// For <see cref="ResponseReadMode.TILL_TIMEOUT"/>
        /// </summary>
        const int READ_CHUNK_SIZE = 30000;
        /// <summary>
        /// For <see cref="ResponseReadMode.TILL_TIMEOUT"/>
        /// </summary>
        const int CHUNK_READ_TIMEOUT = 100; 

        readonly IRequest _deviceRequest;
        readonly ResponseInfo _responseInfo;
        readonly DeviceOperationScope _scope;
        FTDIBoxRequestAddress Address { get; }
        public override IEnumerable<byte> Serialized { get; }
        public override RequestTimeout Timeout { get; } = RequestTimeout.AS_READ_TIMEOUT;

        FTDIBoxRequest(FTDIBoxRequestAddress address, IEnumerable<byte> data, IRequest deviceRequest, DeviceOperationScope scope)
        {
            _deviceRequest = deviceRequest;
            Address = address;
            _scope = scope;

            switch (Address)
            {
                case FTDIBoxRequestAddress.HS_SET_LOW_SPEED_MODE:
                    Serialized =  new byte[] { 0, 16 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.CONSTRAINED_BY_ANSWER, 2, new byte[] { 0, 16 });
                    break;
                case FTDIBoxRequestAddress.LS_SET_HIGH_SPEED_MODE:
                    Serialized = new byte[] { 92, 12 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.CONSTRAINED_BY_ANSWER, 2, new byte[] { 92, 12 });
                    break;
                case FTDIBoxRequestAddress.HS_ACTIVATE_CHANNEL4:
                    Serialized = new byte[] { 0, 20 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.CONSTRAINED_BY_ANSWER, 2, new byte[] { 0, 20 });
                    break;
                case FTDIBoxRequestAddress.LS_ACTIVATE_CHANNEL4:
                    Serialized = new byte[] { 92, 10 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.CONSTRAINED_BY_ANSWER, 2, new byte[] { 92, 10 });
                    break;
                case FTDIBoxRequestAddress.HS_GET_FLASH_LENGTH:
                    Serialized = new byte[] { 9, 128, 170, 206, 0, 0, 0, 0, 0, 2 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.CONSTRAINED_BY_ANSWER, 6, null);
                    break;
                case FTDIBoxRequestAddress.HS_READ_ALL_FLASH:
                    Timeout = RequestTimeout.INFINITY;
                    Serialized = new byte[] { 9, 128, 170, 206, 0, 0, 0, 0, 0, 3 };
                    _responseInfo = new ResponseInfo(ResponseReadMode.TILL_TIMEOUT, ResponseLength.UNKNOWN, null);
                    break;
                case FTDIBoxRequestAddress.LS_DEVICE_REQUEST:
                    Serialized = new Enumerable<byte>()
                    {
                        0xFF, 0x00,
                        data.Count().ToUInt16().SerializeAsNumber(false),
                        data
                    };
                    _responseInfo = new ResponseInfo(ResponseReadMode.READ_BY_TARGET, ResponseLength.UNKNOWN, null);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
        internal static FTDIBoxRequest CreateCommandRequest(FTDIBoxRequestAddress address, DeviceOperationScope scope)
        {
            return new FTDIBoxRequest(address, null, null, scope);
        }
        internal static FTDIBoxRequest CreateDeviceRequest(IRequest request, DeviceOperationScope scope)
        {
            return new FTDIBoxRequest(FTDIBoxRequestAddress.LS_DEVICE_REQUEST, request.Serialized, request, scope);
        }

        protected override async Task<IResponse> deserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo)
        {
            IResponse result = null;
            Stream stream = createReadBuffer();

            var dataRead = new StreamResponseData(stream);
            switch (_responseInfo.ReadMode)
            {
                case ResponseReadMode.TILL_TIMEOUT:
                    try
                    {
                        var readTimeout = new Timeouter(CHUNK_READ_TIMEOUT);
                        while (true)
                        {
                            var piece = await inputStream.WaitAsync(READ_CHUNK_SIZE, WaitMode.NO_MORE_THAN, operationInfo);
                            await stream.WriteAsync(piece, 0, piece.Length);

                            operationInfo.Progress.AddProgress(piece.Length);
                            if (piece.Length != 0)
                            {
                                readTimeout.Restart();
                            }
                            readTimeout.ThrowIfTimeout();
                        }
                    }
                    catch (TimeoutException)
                    {

                    }
                    break;
                case ResponseReadMode.CONSTRAINED_BY_ANSWER:
                    var max = _responseInfo.ResponseFullLength.IsUnknown
                        ? long.MaxValue
                        : _responseInfo.ResponseFullLength.Length;
                    for (long i = 0; i < max; i++)
                    {
                        var data = await inputStream.WaitAsync(1, WaitMode.EXACT, operationInfo);
                        stream.WriteByte(data.Single());
                        operationInfo.Progress.AddProgress(1D / max);
                    }
                    break;
                case ResponseReadMode.READ_BY_TARGET:
                    result = await _deviceRequest.DeserializeResponseAsync(inputStream, operationInfo);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (result == null)
            {
                if (!_responseInfo.ResponseFullLength.IsUnknown &&
                    _responseInfo.ExpectedAnswer != null &&
                    !stream.AsEnumerable().Take((int)_responseInfo.ResponseFullLength.Length).SequenceEqual(_responseInfo.ExpectedAnswer))
                {
                    result = new FTDIBoxResponse(this, RequestStatus.NOT_EXPECTED_RESPONSE_BYTES, dataRead, null, null);
                }
                else
                {
                    await stream.FlushAsync(operationInfo.CancellationToken);

                    IResponseData serviceSection = null;
                    IResponseData bodySection = null;
                    switch (Address)
                    {
                        case FTDIBoxRequestAddress.HS_SET_LOW_SPEED_MODE:
                        case FTDIBoxRequestAddress.LS_SET_HIGH_SPEED_MODE:
                        case FTDIBoxRequestAddress.HS_ACTIVATE_CHANNEL4:
                        case FTDIBoxRequestAddress.LS_ACTIVATE_CHANNEL4:
                            break;

                        case FTDIBoxRequestAddress.HS_GET_FLASH_LENGTH:
                        case FTDIBoxRequestAddress.HS_READ_ALL_FLASH:
                            serviceSection = new ResponseDataAreaProxy(dataRead, dataRead.Count - 6, dataRead.Count);
                            bodySection = new ResponseDataAreaProxy(dataRead, 0, dataRead.Count - 6);
                            break;

                        case FTDIBoxRequestAddress.LS_DEVICE_REQUEST:
                            bodySection = dataRead;
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    result = new FTDIBoxResponse(this, RequestStatus.OK, dataRead, serviceSection, bodySection);
                }
            }
            
            return result;

            Stream createReadBuffer()
            {
                Stream buffer = new MemoryStream();
                if (Address == FTDIBoxRequestAddress.HS_READ_ALL_FLASH)
                {
                    buffer = _scope.TryGetFirstParameter<FlashDumpStreamParameter>()?.Stream;
                    if (buffer == null)
                    {
                        var path = Path.GetTempFileName();
                        buffer = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

                        Logger.LogInfo(null, $"Создан буфер чтения по пути {path}");
                    }
                    else
                    {
                        Logger.LogInfo(null, $"Буфер чтения предоставлен через параметр");
                    }
                }

                return buffer;
            }
        }

        public override IResponse BuildErrorResponse(RequestStatus status)
        {
            return _deviceRequest == null
                ? new FTDIBoxResponse(this, status, null, null, null)
                : _deviceRequest.BuildErrorResponse(status);
        }
    }
}
