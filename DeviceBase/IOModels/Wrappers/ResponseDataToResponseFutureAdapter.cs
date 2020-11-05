using System;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class ResponseDataToResponseFutureAdapter : IResponseFuture
    {
        long _position;
        readonly IResponseData _array;

        public ResponseDataToResponseFutureAdapter(IResponseData array) 
        {
            _array = array;
        }

        public async Task<byte[]> WaitAsync(int count, WaitMode waitMode, AsyncOperationInfo operationInfo)
        {
            var notYetReadBytes = _array.Count - _position;
            switch (waitMode)
            {
                case WaitMode.EXACT:
                    if (notYetReadBytes < count)
                    {
                        throw new TimeoutException();
                    }
                    else
                    {
                        _position += count;
                        
                        return await _array.GetRangeAsync(_position - count, count, operationInfo);
                    }
                case WaitMode.NO_MORE_THAN:
                    var dataToRead = (int)Math.Min(notYetReadBytes, count);
                    _position += dataToRead;

                    return await _array.GetRangeAsync(_position - dataToRead, dataToRead, operationInfo);
                
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
