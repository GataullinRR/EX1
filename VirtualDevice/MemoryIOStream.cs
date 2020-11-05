using System;
using System.Collections.Generic;
using Utilities.Types;
using Utilities.Extensions;
using System.IO;

namespace VirtualDevice
{
    public class IOStream : Stream
    {
        readonly Stream _inputData;
        readonly Stream _outputData;

        public IOStream(Stream inputData, Stream outputData)
        {
            _inputData = inputData ?? throw new ArgumentNullException(nameof(inputData));
            _outputData = outputData ?? throw new ArgumentNullException(nameof(outputData));
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override bool CanTimeout => true;
        public override int ReadTimeout { get; set; } = -1;
        public override int WriteTimeout { get; set; } = -1;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_inputData)
            {
                return _inputData.Read(buffer, offset, count);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_outputData)
            {
                _outputData.Write(buffer.GetRange(offset, count));
            }
        }

        /// <summary>
        /// Reads awailable to read data
        /// </summary>
        /// <returns></returns>
        public byte[] PopInputBuffer()
        {
            lock (_inputData)
            {
                var data = _inputData.ReadToEnd();

                return data;
            }
        }

        public void WriteInputData(IEnumerable<byte> data)
        {
            lock (_inputData)
            {
                var oldPosition = _inputData.Position;
                _inputData.Seek(0, SeekOrigin.End);
                _inputData.Write(data);
                _inputData.Position = oldPosition;
            }
        }
    }
}
