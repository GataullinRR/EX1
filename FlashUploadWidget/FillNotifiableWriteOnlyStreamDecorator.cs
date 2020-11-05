using System;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;
using System.IO;
using System.Threading;

namespace FlashUploadWidget
{
    class FillNotifiableWriteOnlyStreamDecorator : StreamProxyBase
    {
        public event Action LengthChangedAsync;

        long _lengthDelta;
        long _oldLength = 0;

        public FillNotifiableWriteOnlyStreamDecorator(long lengthDelta, Stream baseStream) : base(baseStream)
        {
            _lengthDelta = lengthDelta;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            update();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await base.WriteAsync(buffer, offset, count, cancellationToken);

            update();
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);

            update();
        }

        void update()
        {
            if (Length - _oldLength > _lengthDelta)
            {
                _oldLength = Length;
                Task.Run(() => LengthChangedAsync?.Invoke());
            }
        }
    }
}
