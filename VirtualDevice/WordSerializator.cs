using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;

namespace VirtualDevice
{
    class WordSerializator
    {
        public byte[] Serialize(ushort value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public byte[] Serialize(short value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public ushort Deserialize(IEnumerable<byte> data)
        {
            return BitConverter.ToUInt16(data.Take(2).Reverse().ToArray(), 0);
        }
    }
}