using System;
using System.Collections.Generic;

namespace DeviceBase.IOModels
{
    abstract class RichResponseDataBase
    {
        public IEnumerable<byte> Raw { get; }

        protected RichResponseDataBase(IEnumerable<byte> raw)
        {
            Raw = raw ?? throw new ArgumentNullException(nameof(raw));
        }
    }
}
