using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.Runtime.InteropServices;

namespace RUSTelemetryStreamSenderWidget
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    struct RegData
    {
        [FieldOffset(0)]
        DateTime _time;
        [FieldOffset(8)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        ushort[] _data;

        public DateTime Time => _time;
        public ushort[] Row => _data;

        public static RegData FromBytes(byte[] array, int startIndex)
        {
            RegData value = new RegData();
            value._time = DateTime.FromBinary(BitConverter.ToInt64(array, startIndex));
            value._data = new ushort[6];
            Buffer.BlockCopy(array, startIndex + 8, value.Row, 0, 6 * 2);
            
            return value;
        }
    }
}
