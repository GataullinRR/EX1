using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;
using System.Linq;

namespace DeviceBase.IOModels
{
    class LoggingResponseFutureDecorator : ResponseFutureProxyBase
    {
        readonly Enumerable<byte> _storage = new Enumerable<byte>();

        public int Capacity { get; }
        public long ReadCount { get; private set; }
        public int StorageCount { get; private set; }
        public IEnumerable<byte> Storage => _storage;

        public LoggingResponseFutureDecorator(IResponseFuture @base, int storageCapacity) : base(@base)
        {
            Capacity = storageCapacity;
        }

        public override async Task<byte[]> WaitAsync(int count, WaitMode waitMode, AsyncOperationInfo operationInfo)
        {
            var result = await base.WaitAsync(count, waitMode, operationInfo);

            ReadCount += result.Length;
            var bytesToStore = Math.Min(Capacity - (ReadCount - result.Length), result.Length).NegativeToZero().ToInt32();
            if (bytesToStore != 0)
            {
                _storage.Add(result.Take(bytesToStore));
                StorageCount += bytesToStore;
            }

            return result;
        }
    }
}
