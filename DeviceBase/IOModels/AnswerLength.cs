using System;

namespace DeviceBase.IOModels
{
    public class ResponseLength
    {
        public static implicit operator ResponseLength(long length)
        {
            return new ResponseLength(length);
        }

        public static readonly ResponseLength UNKNOWN = new ResponseLength(null);

        readonly long? _Length;

        public long Length => IsUnknown
            ? throw new NotSupportedException("The length is Unknown!")
            : _Length.Value;
        public bool IsUnknown => !_Length.HasValue;

        public ResponseLength(long length) 
            : this((long?)length)
        {

        }
        ResponseLength(long? length)
        {
            _Length = length;
        }
    }
}
