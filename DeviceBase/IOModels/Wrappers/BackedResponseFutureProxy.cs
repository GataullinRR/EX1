using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class BackedResponseFutureDecorator : ResponseFutureProxyBase
    {
        public List<byte> Storage { get; } = new List<byte>();

        public BackedResponseFutureDecorator(IResponseFuture @base) : base(@base)
        {

        }

        public override async Task<byte[]> WaitAsync(int count, WaitMode mode, AsyncOperationInfo operationInfo)
        {
            var data = await base.WaitAsync(count, mode, operationInfo);
            Storage.AddRange(data);
            
            return data;
        }
    }
}
