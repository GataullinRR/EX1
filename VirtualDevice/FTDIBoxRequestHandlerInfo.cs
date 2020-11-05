using System;
using System.Collections.Generic;

namespace VirtualDevice
{
    class FTDIBoxRequestHandlerInfo
    {
        public delegate IEnumerable<byte> HandleDelegate(byte[] requestBody);

        public HandleDelegate Handler { get; }
        public FTDIBoxCommandHandlerAttribute Info { get; }

        public FTDIBoxRequestHandlerInfo(HandleDelegate handler, FTDIBoxCommandHandlerAttribute info)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }
    }
}
