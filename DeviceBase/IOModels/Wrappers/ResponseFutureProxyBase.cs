using System;
using System.Collections;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class ResponseFutureProxyBase : IResponseFuture
    {
        readonly IResponseFuture _base;

        public ResponseFutureProxyBase(IResponseFuture @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual Task<byte[]> WaitAsync(int count, WaitMode waitMode, AsyncOperationInfo operationInfo)
        {
            return _base.WaitAsync(count, waitMode, operationInfo);
        }
    }
}
