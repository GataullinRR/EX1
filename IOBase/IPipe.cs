using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;

namespace IOBase
{
    public enum WriteStatus
    {
        [StateType(true)]
        DONE,
        [StateType(true)]
        PATIALLY_DONE,
        [StateType(false)]
        UNKNOWN_ERROR
    }

    public enum ReadStatus
    {
        [StateType(true)]
        DONE,
        [StateType(true)]
        PATIALLY_DONE,
        [StateType(false)]
        UNKNOWN_ERROR
    }
    
    public class PipeWriteResult
    {
        public PipeWriteResult(WriteStatus status) : this(status, null)
        {

        }

        public PipeWriteResult(WriteStatus status, int? bytesWritten)
        {
            Status = status;
            BytesWritten = bytesWritten;
        }

        public WriteStatus Status { get; }
        public int? BytesWritten { get; }
    }

    public class PipeReadResult
    {
        public PipeReadResult(ReadStatus status) : this(status, null, new byte[0])
        {

        }
        public PipeReadResult(ReadStatus status, int? bytesRead, IEnumerable<byte> data)
        {
            Status = status;
            BytesRead = bytesRead;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public ReadStatus Status { get; }
        public int? BytesRead { get; }
        public IEnumerable<byte> Data { get; }
    }

    public class PipeReadParameters
    {
        public PipeReadParameters(int bytesExpected)
        {
            BytesExpected = bytesExpected;
        }

        public int BytesExpected { get; }
    }

    public class PipeWriteParameters
    {
        public PipeWriteParameters(IEnumerable<byte> buffer)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public IEnumerable<byte> Buffer { get; }
    }

    public interface IPipe
    {
        int ReadTimeout { get; }
        int WriteTimeout { get; }

        Task<PipeWriteResult> WriteAsync(PipeWriteParameters parameters, CancellationToken cancellation);
        Task<PipeReadResult> ReadAsync(PipeReadParameters parameters, CancellationToken cancellation);
        Task<PipeReadResult> ClearReadBufferAsync(CancellationToken cancellation);
    }
}
