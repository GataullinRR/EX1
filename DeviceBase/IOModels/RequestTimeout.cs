using System;

namespace DeviceBase.IOModels
{
    public class RequestTimeout
    {
        public static readonly RequestTimeout AS_READ_TIMEOUT = new RequestTimeout() { _timeout = null };
        public static readonly RequestTimeout INFINITY = new RequestTimeout() { _timeout = int.MaxValue };

        int? _timeout;
        public int Timeout => _timeout ?? throw new NotSupportedException();
        public bool AsReadTimeout => _timeout == null;

        RequestTimeout() { }

        public static RequestTimeout Create(int timeout)
        {
            return new RequestTimeout()
            {
                _timeout = timeout
            };
        }
    }
}
