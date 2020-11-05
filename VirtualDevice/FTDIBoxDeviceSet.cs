using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;
using DeviceBase.Models;
using DeviceBase;
using DeviceBase.IOModels;
using System.Reflection;
using System;
using TinyConfig;
using System.Threading;
using IOBase;
using System.Threading.Tasks;
using VirtualDevice.Devices;
using Common;

namespace VirtualDevice
{
    public class FTDIBoxDeviceSet : IOStream, IPipe
    {
        //readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("FTDIBoxDeviceSet");
        //readonly static ConfigProxy<bool> SIMULATE_CONNECTION_SPEED = CONFIG.Read(true);
        //readonly static ConfigProxy<int> BAUD = CONFIG.Read(int.MaxValue);

        const int MAX_REQUEST_LENGTH = 50000;

        readonly FTDIBox _device;
        readonly List<byte> _buffer = new List<byte>();

        public FTDIBoxDeviceSet() : base(Storaging.GetTempFileStream(), Storaging.GetTempFileStream())
        {
            ReadTimeout = 3000;
            
            _device = (FTDIBox)Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t == typeof(FTDIBox))
                .Select(t => Activator.CreateInstance(t))
                .Single();
        }

        public async Task<PipeReadResult> ReadAsync(PipeReadParameters parameters, CancellationToken cancellation)
        {
            var buffer = new byte[parameters.BytesExpected];
            var bytesRead = Read(buffer, 0, buffer.Length);
            var status = bytesRead == buffer.Length
                ? IOBase.ReadStatus.DONE
                : IOBase.ReadStatus.PATIALLY_DONE;

            return new PipeReadResult(status, bytesRead, buffer.Take(bytesRead));
        }
        public async Task<PipeReadResult> ClearReadBufferAsync(CancellationToken cancellation)
        {
            var data = PopInputBuffer();

            return new PipeReadResult(IOBase.ReadStatus.DONE, data.Length, data);
        }

        public async Task<PipeWriteResult> WriteAsync(PipeWriteParameters parameters, CancellationToken cancellation)
        {
            var data = await parameters.Buffer.ToArrayAsync();
            Write(data, 0, data.Length);

            return new PipeWriteResult(WriteStatus.DONE, data.Length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = buffer.GetRange(offset, count);
            var answer = handle(data).ToArray();
            //Thread.Sleep(((buffer.Length + answer.Length) * 8D / BAUD * 1000).Ceiling());

            WriteInputData(answer);
        }

        IEnumerable<byte> handle(IEnumerable<byte> data)
        {
            _buffer.AddRange(data);

            var answer = handleDevices().ToArray();
            _buffer.RemoveRange(0, (_buffer.Count - MAX_REQUEST_LENGTH).NegativeToZero());
            return answer;

            IEnumerable<byte> handleDevices()
            {
                foreach (var handlerInfo in getHandlers().OrderBy(h => _buffer.Find(h.Info.RequestBytes).Index))
                {
                    var start = _buffer.Find(handlerInfo.Info.RequestBytes);
                    if (start.Found)
                    {
                        _buffer.RemoveRange(0, start.Index); // All bytes before request bytes are trash
                    }
                    var headerLength = handlerInfo.Info.RequestBytes.Length + 
                        (handlerInfo.Info.HasLenghtField ? 2 : 0);
                    var isHeaderArrived = _buffer.Count >= headerLength;
                    if (start.Found && isHeaderArrived)
                    {
                        var bodyLength = handlerInfo.Info.HasLenghtField
                            ? _buffer.Skip(headerLength - 2).DeserializeUInt16(true)
                            : 0;
                        var requestLength = headerLength + bodyLength;
                        if (_buffer.Count < requestLength)
                        {
                            return Enumerable.Empty<byte>(); // wait till there will be enough amount of data
                        }
                        byte[] requestBody = _buffer
                            .Skip(headerLength)
                            .Take(bodyLength)
                            .ToArray();
                       var response = handlerInfo.Handler(requestBody).ToArray();
                        _buffer.RemoveRange(0, requestLength);

                        return response;
                    }
                }

                return Enumerable.Empty<byte>();

                IEnumerable<FTDIBoxRequestHandlerInfo> getHandlers()
                {
                    return _device.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Select(mi => (MI: mi, Info: mi.GetCustomAttribute<FTDIBoxCommandHandlerAttribute>()))
                        .Where(i => i.Info != null)
                        .Select(rh => new FTDIBoxRequestHandlerInfo(
                            (FTDIBoxRequestHandlerInfo.HandleDelegate)Delegate.CreateDelegate(typeof(FTDIBoxRequestHandlerInfo.HandleDelegate), _device, rh.MI),
                            rh.Info));
                }
            }
        }
    }
}
