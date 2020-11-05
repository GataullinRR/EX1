using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class StreamResponseData : IResponseData
    {
        readonly Stream _base;

        public long Count => _base.Length;

        public StreamResponseData(Stream baseStream) 
        {
            _base = baseStream;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            _base.Position = 0;

            return _base.AsEnumerable().GetEnumerator();
        }

        public Task WriteAsync(byte[] data)
        {
            _base.Position = _base.Length;

            return _base.WriteAsync(data, 0, data.Length);
        }

        public async Task<byte[]> GetRangeAsync(long from, int count, AsyncOperationInfo operationInfo)
        {
            _base.Position = from;

            return await _base.ReadAsync(count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public async Task<ReadOnlyCollection<byte>> GetAllAsync(AsyncOperationInfo operationInfo)
        {
            var allData = await GetRangeAsync(0, checked((int)Count), operationInfo);

            return new ReadOnlyCollection<byte>(allData);
        }
    }
}
