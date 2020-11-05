using FTD2XXSerialPort;
using IOBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using static FTD2XXSerialPort.FTDI;

namespace FTD2XXSerialPort
{
    class FTDIPipe : IPipe
    {
        readonly FTDI _ftdi;

        public int ReadTimeout { get; } 
        public int WriteTimeout { get; }

        public FTDIPipe(FTDI ftdi, int readTimeout, int writeTimeout)
        {
            _ftdi = ftdi ?? throw new ArgumentNullException(nameof(ftdi));
            ReadTimeout = readTimeout;
            WriteTimeout = writeTimeout;
        }

        public async Task<PipeWriteResult> WriteAsync(PipeWriteParameters parameters, CancellationToken cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);

            var buffer = parameters.Buffer.ToArray();
            uint bytesWritten = 0;
            var status = _ftdi.Write(parameters.Buffer.ToArray(), buffer.Length.ToUInt32(), ref bytesWritten);
            if (status != FT_STATUS.FT_OK)
            {
                return new PipeWriteResult(WriteStatus.UNKNOWN_ERROR);
            }
            else
            {
                if (bytesWritten != buffer.Length)
                {
                    return new PipeWriteResult(WriteStatus.PATIALLY_DONE, bytesWritten.ToInt32());
                }
                else
                {
                    return new PipeWriteResult(WriteStatus.DONE, bytesWritten.ToInt32());
                }
            }
        }

        public async Task<PipeReadResult> ReadAsync(PipeReadParameters parameters, CancellationToken cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);

            var buffer = new byte[parameters.BytesExpected];
            uint bytesRead = 0;
            var status = _ftdi.Read(buffer, parameters.BytesExpected.ToUInt32(), ref bytesRead);
            if (status == FT_STATUS.FT_OK)
            {
                if (bytesRead < parameters.BytesExpected)
                {
                    return new PipeReadResult(ReadStatus.PATIALLY_DONE, bytesRead.ToInt32(), buffer.Take(bytesRead.ToInt32()));
                }
                else if (bytesRead == parameters.BytesExpected)
                {
                    return new PipeReadResult(ReadStatus.DONE, bytesRead.ToInt32(), buffer);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return new PipeReadResult(ReadStatus.UNKNOWN_ERROR);
            }
        }

        public async Task<PipeReadResult> ClearReadBufferAsync(CancellationToken cancellation)
        {
            var buffer = new List<byte>();
            int bytesRead = -1;
            while(bytesRead != 0)
            {
                var result = await ReadAsync(new PipeReadParameters(1000), cancellation);
                if (result.Status.IsSuccessState())
                {
                    bytesRead = result.BytesRead.Value;
                    var data = result.Data.Take(result.BytesRead.Value).ToArray();
                    buffer.AddRange(data);
                }
                else
                {
                    return new PipeReadResult(ReadStatus.UNKNOWN_ERROR, buffer.Count, buffer);
                }

                cancellation.ThrowIfCancellationRequested();
            }

            return new PipeReadResult(ReadStatus.DONE, buffer.Count, buffer);
        }
    }
}
