using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Extensions;
using System.Linq;
using System;
using System.Collections.ObjectModel;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class InMemoryResponseData : IResponseData
    {
        readonly IList<byte> _data;

        public long Count => _data.Count;

        public InMemoryResponseData(IList<byte> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public async Task<byte[]> GetRangeAsync(long from, int count, AsyncOperationInfo operationInfo)
        {
            return _data.GetRange((int)from, count).ToArray();
        }

        public async Task<ReadOnlyCollection<byte>> GetAllAsync(AsyncOperationInfo operationInfo)
        {
            return new ReadOnlyCollection<byte>(_data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
