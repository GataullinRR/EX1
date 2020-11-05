using System;
using System.IO.Ports;
using System.Threading;
using Utilities.Extensions;
using IOBase;
using System.Threading.Tasks;
using Utilities;
using System.Linq;

namespace RUSManagingTool.Models
{
    class COMPortPipe : IPipe
    {
        readonly SerialPort _port;

        public int ReadTimeout => _port.ReadTimeout;
        public int WriteTimeout => _port.WriteTimeout;

        public COMPortPipe(SerialPort port)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public async Task<PipeReadResult> ClearReadBufferAsync(CancellationToken cancellation)
        {
            var bytesToRead = _port.BytesToRead;
            var buffer = new byte[bytesToRead];
            var bytesRead = _port.Read(buffer, 0, bytesToRead);

            return new PipeReadResult(ReadStatus.DONE, bytesRead, buffer);
        }

        public async Task<PipeReadResult> ReadAsync(PipeReadParameters parameters, CancellationToken cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull();

            var buffer = new byte[parameters.BytesExpected];
            for (int i = 0; i < buffer.Length; i++)
            {
                var data = _port.ReadByte();
                if (data == -1)
                {
                    await Task.Delay(1, cancellation);

                    i--;
                }
                else
                {
                    buffer[i] = data.ToByte();
                }

                cancellation.ThrowIfCancellationRequested();
            }

            return new PipeReadResult(ReadStatus.DONE, buffer.Length, buffer);
        }

        public async Task<PipeWriteResult> WriteAsync(PipeWriteParameters parameters, CancellationToken cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull();

            var buffer = parameters.Buffer.ToArray();
            _port.Write(buffer, 0, buffer.Length);

            return new PipeWriteResult(WriteStatus.DONE, buffer.Length);
        }
    }
}