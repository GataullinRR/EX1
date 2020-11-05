using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class ResponseDataAreaProxy : ResponseDataProxyBase
    {
        readonly long _from;
        readonly long _to;

        public ResponseDataAreaProxy(IResponseData @base, long from, long to) : base(@base)
        {
            _from = from;
            _to = to;
        }

        public override long Count => _to - _from;

        public override Task<byte[]> GetRangeAsync(long from, int count, AsyncOperationInfo operationInfo)
        {
            return base.GetRangeAsync(_from + from, count, operationInfo);
        }

        public override async Task<ReadOnlyCollection<byte>> GetAllAsync(AsyncOperationInfo operationInfo)
        {
            var allData = await GetRangeAsync(0, checked((int)Count), operationInfo);

            return new ReadOnlyCollection<byte>(allData);
        }

        public override IEnumerator<byte> GetEnumerator()
        {
            var i = _from;
            var iterator = base.GetEnumerator();
            while (iterator.MoveNext())
            {
                i--;
                if (i == 0)
                {
                    break;
                }
            }

            var maxCount = Count;
            while (iterator.MoveNext())
            {
                maxCount--;
                if (maxCount != -1)
                {
                    yield return iterator.Current;
                }
            }
        }
    }
}
