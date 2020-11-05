using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class ResponseDataProxyBase : IResponseData
    {
        readonly IResponseData _base;

        public virtual long Count => _base.Count;

        public ResponseDataProxyBase(IResponseData @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual IEnumerator<byte> GetEnumerator()
        {
            return _base.GetEnumerator();
        }

        public virtual Task<byte[]> GetRangeAsync(long from, int count, AsyncOperationInfo operationInfo)
        {
            return _base.GetRangeAsync(from, count, operationInfo);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual Task<ReadOnlyCollection<byte>> GetAllAsync(AsyncOperationInfo operationInfo)
        {
            return _base.GetAllAsync(operationInfo);
        }
    }
}
