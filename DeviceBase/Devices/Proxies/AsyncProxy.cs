using DeviceBase.IOModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class AsyncProxy : RUSDeviceProxyBase
    {
        public AsyncProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override async Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);

            await _base.ActivateDeviceAsync(scope, cancellation);
        }

        public override async Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);
              
            await _base.DeactivateDeviceAsync(scope, cancellation);
        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);
              
            return await _base.ReadAsync(request, scope, cancellation);
        }

        public override async Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);
              
            return await _base.BurnAsync(request, entities, scope, cancellation);
        }

        public override async Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);
               
            return await _base.TryReadStatusAsync(scope, cancellation);
        }
    }
}
