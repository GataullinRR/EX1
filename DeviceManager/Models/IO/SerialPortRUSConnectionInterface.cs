using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase;
using TinyConfig;
using System.IO;
using DeviceBase.IOModels;
using Common;
using System.Threading;
using IOBase;
using DeviceBase.IOModels.Protocols;
using System.Collections;

namespace RUSManagingTool.Models
{
    class SerialPortRUSConnectionInterface : IRUSConnectionInterface
    {
        class ResponseFuture : Disposable, IResponseFuture
        {
            readonly IPipe _pipe;
            readonly CancellationToken _cancellation;
            readonly Timeouter _responseReadTimeout;
            readonly Timeouter _waitTimeout;

            public ResponseFuture(IPipe pipe, RequestTimeout timeoutInfo, CancellationToken cancellation)
            {
                _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
                _cancellation = cancellation;
                _responseReadTimeout = new Timeouter(timeoutInfo.AsReadTimeout ? _pipe.ReadTimeout : timeoutInfo.Timeout);
                _waitTimeout = new Timeouter(_pipe.ReadTimeout);
            }

            public async Task<byte[]> WaitAsync(int count, WaitMode mode, AsyncOperationInfo operationInfo)
            {
                throwIfDisposed();
                _responseReadTimeout.ThrowIfTimeout();
                operationInfo.CancellationToken.ThrowIfCancellationRequested();

                byte[] waitResult = null;
                try
                {
                    using (_waitTimeout.RestartMode)
                    {
                        switch (mode)
                        {
                            case WaitMode.EXACT:
                                var buffer = new List<byte>(count);
                                while (buffer.Count != count)
                                {
                                    _responseReadTimeout.ThrowIfTimeout();

                                    var result = await _pipe.ReadAsync(new PipeReadParameters(count - buffer.Count), _cancellation);
                                    if (result.Status.IsOneOf(IOBase.ReadStatus.DONE, IOBase.ReadStatus.PATIALLY_DONE))
                                    {
                                        buffer.AddRange(result.Data);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                waitResult = buffer.ToArray();
                                break;

                            case WaitMode.NO_MORE_THAN:
                                {
                                    var result = await _pipe.ReadAsync(new PipeReadParameters(count), _cancellation);
                                    if (result.Status.IsOneOf(IOBase.ReadStatus.DONE, IOBase.ReadStatus.PATIALLY_DONE))
                                    {
                                        waitResult = result.Data.ToArray();
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                    }
                    
                    return waitResult;
                }
                finally
                {
                    await Logger.LogResponseAsync(waitResult ?? new byte[0], count);
                }
            }

            protected override void disposeManagedState()
            {
                
            }
        }

        readonly IInterface _port;

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public InterfaceDevice InterfaceDevice => _port.CurrentInterfaceDevice;

        public IEnumerable<Protocol> SupportedProtocols { get; } = new Protocol[] 
        { 
            Protocol.SALAHOV 
        };

        public SerialPortRUSConnectionInterface(IInterface port)
        {
            _port = port;

            _port.ConnectionEstablished += _base_ConnectionEstablished;
            _port.ConnectionClosed += _base_ConnectionClosed;

            void _base_ConnectionClosed(object sender, EventArgs e)
            {
                ConnectionClosed?.Invoke(sender, e);
            }
            void _base_ConnectionEstablished(object sender, EventArgs e)
            {
                ConnectionEstablished?.Invoke(sender, e);
            }
        }

        public async Task<IResponse> RequestAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            IResponse result = null;
            await clearUnreadBytesAsync();

            var requestData = request.Serialized.ToArray();
            var sendRequestOk = await tryWriteToPort(requestData, operationInfo);
            if (!sendRequestOk)
            {
                result = request.BuildErrorResponse(RequestStatus.CONNECTION_INTERFACE_ERROR);
            }
            else
            {
                await Logger.LogRequestAsync(requestData);

                try
                {
                    using (var future = new ResponseFuture(_port.Pipe, request.Timeout, operationInfo))
                    {
                        result = await request.DeserializeResponseAsync(future, operationInfo);
                    }
                }
                catch (TimeoutException)
                {
                    result = request.BuildErrorResponse(RequestStatus.READ_TIMEOUT);
                }
                catch (IOException)
                {
                    result = request.BuildErrorResponse(RequestStatus.CONNECTION_INTERFACE_ERROR);
                }
                catch (OperationCanceledException)
                {
                    result = request.BuildErrorResponse(RequestStatus.CONNECTION_INTERFACE_ERROR);
                }
                catch
                {
                    result = request.BuildErrorResponse(RequestStatus.UNKNOWN_ERROR);
                }
            }

            return result;

            async Task clearUnreadBytesAsync()
            {
                var clearOperation = await CommonUtils.TryAsync(async () => await _port.Pipe.ClearReadBufferAsync(operationInfo));
                if (clearOperation.Ok)
                {
                    if (clearOperation.Result.BytesRead != 0)
                    {
                        Logger.LogWarning(null, $"Обнаружены несчитанные данные в буфере приема! Это означает, что устройство отправило больше данных чем предполагалось. -NLДлина: {clearOperation.Result.Data.Count()} -NLДанные={clearOperation.Result.Data.Take(1000).Select(b => b.ToString("X2")).AsString(" ")}");
                    }
                }
                else
                {
                    Logger.LogError(null, $"Ошибка очистки буфера чтения порта. Порт был закрыт?");
                }
            }
        }

        async Task<bool> tryWriteToPort(IEnumerable<byte> data, CancellationToken cancellation)
        {
            if (data == null)
            {
                throw new NullReferenceException();
            }

            var dataArray = data.ToArray();
            Exception ex = null;
            PipeWriteResult result = null;
            try
            {
                result = await _port.Pipe.WriteAsync(new PipeWriteParameters(dataArray), cancellation);
            }
            catch (Exception e)
            {
                ex = e;
            }
            using (Logger.Indent)
            {
                var wasWritten = ex == null 
                    && result.Status == WriteStatus.DONE;
                Action<string, Exception> logDelegate = wasWritten
                    ? (Action<string, Exception>)((msg, e) => Logger.LogInfo(null, msg))
                    : (Action<string, Exception>)((msg, e) => Logger.LogError(null, msg, e));
                logDelegate($"Отправка на {_port.PortName} Длина:{dataArray.Length}" +
                    $"{Global.NL}Данные<HEX>:{dataArray.Select(b => b.ToString("X2").PadLeft(3)).Aggregate(" ")}" +
                    $"{Global.NL}Данные<DEC>:{dataArray.Select(b => b.ToString("D3").PadLeft(3)).Aggregate(" ")}" +
                    $"{Global.NL}Завершена {wasWritten.Ternar("успешно", "с ошибкой".ToUpperInvariant())}", ex);
                
                return wasWritten;
            }
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellation)
        {
            return await _port.Locker.AcquireAsync(cancellation);
        }
    }
}