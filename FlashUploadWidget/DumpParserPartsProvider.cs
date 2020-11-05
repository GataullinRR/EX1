using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FlashDumpLoaderExports;

namespace FlashUploadWidget
{
    class DumpParserPartsProvider
    {
        class Part
        {
            public Part(long position, long length)
            {
                Position = position;
                Length = length;
            }

            public long Position { get; }
            public long Length { get; }
        }

        readonly ConcurrentQueue<Part> _parts = new ConcurrentQueue<Part>();
        readonly string _path;
        bool _done;

        public DumpParserPartsProvider(string path)
        {
            _path = path;
        }

        public async Task ProvideNextAndWaitTillFinishesAsync(long position, long length, AsyncOperationInfo operationInfo)
        {
            _parts.Enqueue(new Part(position, length));
            while (_parts.TryPeek(out _))
            {
                await Task.Delay(100, operationInfo);
            }
        }

        public void Complete()
        {
            _done = true;
        }

        public IEnumerable<OpenStreamAsyncDelegate> RawDumpParts()
        {
            while (!_done)
            {
                Part part = null;
                while (!_parts.TryDequeue(out part))
                {
                    Thread.Sleep(100);
                }
                yield return (OpenStreamAsyncDelegate)((sp, oi) => openStream(part, sp, oi));
            }
        }

        async Task<Stream> openStream(Part part, StreamParameters parameters, AsyncOperationInfo operationInfo)
        {
            var baseStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, parameters.ReadBufferSize);
            baseStream.Position = part.Position;
            return new SectionedStreamProxy(baseStream, part.Length);
        }
    }
}
