using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
    public class InMemoryResponseFuture : IResponseFuture
    {
        readonly IList<byte> _response;
        int _position = 0;

        public InMemoryResponseFuture(IList<byte> response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public async Task<byte[]> WaitAsync(int count, WaitMode waitMode, AsyncOperationInfo operationInfo)
        {
            var maxCount = _response.Count - _position;
            switch (waitMode)
            {
                case WaitMode.EXACT:
                    if (maxCount < count)
                    {
                        throw new TimeoutException();
                    }
                    else
                    {
                        var result = _response.GetRange(_position, count).ToArray();
                        _position += count;

                        return result;
                    }
                case WaitMode.NO_MORE_THAN:
                    var msxAvailableBytes = Math.Min(maxCount, _position + count) - _position;
                    var bytesToRead = Math.Min(count, msxAvailableBytes);
                    _position += bytesToRead;
                    return _response.GetRange(_position, bytesToRead).ToArray();
             
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
