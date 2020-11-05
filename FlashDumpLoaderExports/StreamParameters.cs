using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public delegate Task<Stream> OpenStreamAsyncDelegate(StreamParameters parameters, AsyncOperationInfo operationInfo);

    public class StreamParameters
    {
        public int ReadBufferSize { get; }

        public StreamParameters(int readBufferSize)
        {
            ReadBufferSize = readBufferSize;
        }
    }
}
