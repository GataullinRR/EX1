using System.Collections.Generic;
using System.Linq;
using Utilities.Types;
using Utilities.Extensions;
using System.Reflection;
using System;
using TinyConfig;
using System.Threading;
using DeviceBase.Helpers;
using IOBase;
using System.Threading.Tasks;
using Common;

namespace VirtualDevice
{
    public class SalachovDeviceSet : IOStream, IPipe
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("SalachovDeviceSet");
        readonly static ConfigProxy<bool> SIMULATE_CONNECTION_SPEED = CONFIG.Read(true);
        readonly static ConfigProxy<int> BAUD = CONFIG.Read(230400);

        const int MINIMAL_REQUEST_LENGTH = 6;
        readonly CRC16CCITT _crc16Calculator = new CRC16CCITT();

        readonly WordSerializator _serializator = new WordSerializator();
        readonly VirtualRUSDeviceBase[] _devices;
        readonly List<byte> _buffer = new List<byte>();

        public SalachovDeviceSet() : base(Storaging.GetTempFileStream(), Storaging.GetTempFileStream())
        {
            ReadTimeout = 3000;

            _devices = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(VirtualRUSDeviceBase)) && !t.IsAbstract)
                .Select(t => (VirtualRUSDeviceBase)Activator.CreateInstance(t, _serializator))
                .OrderByDescending(d => d is IRUSModule)
                .ToArray();
            foreach (var module in _devices.Select(d => d as IRUSModule).SkipNulls())
            {
                module.ChildrenInterfaceLine = new SalachovDeviceSet(_devices);
            }
        }
        SalachovDeviceSet(VirtualRUSDeviceBase[] devices) : base(Storaging.GetTempFileStream(), Storaging.GetTempFileStream())
        {
            _devices = devices;
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
            Thread.Sleep(((buffer.Length + answer.Length) * 8D / BAUD * 1000).Ceiling());

            WriteInputData(answer);
        }

        IEnumerable<byte> handle(IEnumerable<byte> data)
        {
            _buffer.AddRange(data);

            while (_buffer.Count >= MINIMAL_REQUEST_LENGTH)
            {
                var answer = handleDevices(_devices).ToArray();
                if (answer.Length == 0) // nothing is found
                {
                    _buffer.RemoveRange(0, (_buffer.Count - MINIMAL_REQUEST_LENGTH + 1).NegativeToZero());
                }
                else
                {
                    return answer;
                }

                IEnumerable<byte> handleDevices(IEnumerable<VirtualRUSDeviceBase> devices)
                {
                    foreach (var device in devices)
                    {
                        var isReadRequest = true;
                        foreach (var handlerInfo in getHandlers(device).DublicateEach(2)) // First we see for read commands, then for burn
                        {
                            var requestBytes = buildRequest((byte)device.Id, handlerInfo.Address.GetInfo().Address, isReadRequest);
                            var start = _buffer.Find(requestBytes);
                            if (start.Found)
                            {
                                _buffer.RemoveRange(0, start.Index); // All bytes before request bytes are trash
                                byte[] request = _buffer.Take(MINIMAL_REQUEST_LENGTH).ToArray();
                                byte[] requestBody = null;
                                if (isReadRequest)
                                {
                                    _buffer.RemoveRange(0, MINIMAL_REQUEST_LENGTH); // Remove request from the buffer, because it'll be handled
                                }
                                else
                                {
                                    // Write requests always have length field
                                    var bodyLength = _serializator.Deserialize(_buffer.Skip(MINIMAL_REQUEST_LENGTH - 2));
                                    var fullLength = MINIMAL_REQUEST_LENGTH // Header + body length/CRC16
                                        + bodyLength
                                        + 2; // CRC16 Length
                                    if (_buffer.Count >= fullLength) // received completely
                                    {
                                        request = _buffer.Take(fullLength).ToArray();
                                        requestBody = _buffer.Skip(MINIMAL_REQUEST_LENGTH)
                                            .Take(bodyLength)
                                            .ToArray();
                                        _buffer.RemoveRange(0, fullLength);
                                    }
                                    else // we are waiting for the packet to be written completely in the next function call
                                    {
                                        return Enumerable.Empty<byte>();
                                    }
                                }

                                var packet = request.Take(MINIMAL_REQUEST_LENGTH - 2); // Header
                                var answerBody = handlerInfo.Handler(requestBody, isReadRequest);
                                if (answerBody != null) // No data in answer
                                {
                                    packet = packet
                                        .Concat(_serializator.Serialize(answerBody.Length.ToUInt16()))
                                        .Concat(answerBody);
                                }
                                var crc = _crc16Calculator.ComputeChecksum(packet.ToArray());
                                packet = packet.Concat(_serializator.Serialize(crc));

                                return packet.ToArray();
                            }

                            isReadRequest = !isReadRequest;
                        }
                    }

                    return Enumerable.Empty<byte>();

                    IEnumerable<RequestHandlerInfo> getHandlers(VirtualRUSDeviceBase vd)
                    {
                        return vd.GetType()
                            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Select(mi => (MI: mi, Info: mi.GetCustomAttribute<SalachovCommandHandlerAttribute>()))
                            .Where(i => i.Info != null)
                            .Select(rh => new RequestHandlerInfo(
                                rh.Info.Address,
                                (RequestHandlerInfo.HandleDelegate)Delegate.CreateDelegate(typeof(RequestHandlerInfo.HandleDelegate), vd, rh.MI)));
                    }
                }
            }

            return Enumerable.Empty<byte>();
        }

        byte[] buildRequest(byte id, byte command, bool isReadCommand)
        {
            var bytes = new Enumerable<byte>()
                {
                    id.BitLShift(1),
                    (isReadCommand ? 0 : (1 << 7)).ToByte().BitOR(command),
                    0,
                    0
                };
            if (isReadCommand)
            {
                bytes.Add(_serializator.Serialize(_crc16Calculator.ComputeChecksum(bytes)));
            }

            return bytes.ToArray();
        }
    }
}
