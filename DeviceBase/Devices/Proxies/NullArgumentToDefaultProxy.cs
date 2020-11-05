using DeviceBase.IOModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class NullArgumentToDefaultProxy : RUSDeviceProxyBase
    {
        public NullArgumentToDefaultProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.ActivateDeviceAsync(scope ?? DeviceOperationScope.DEFAULT, cancellation ?? new AsyncOperationInfo());
        }

        public override async Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await _base.DeactivateDeviceAsync(scope ?? DeviceOperationScope.DEFAULT, cancellation ?? new AsyncOperationInfo());
        }

        public override Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.ReadAsync(request, scope ?? DeviceOperationScope.DEFAULT, cancellation ?? new AsyncOperationInfo());
        }

        public override Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.BurnAsync(request, entities, scope ?? DeviceOperationScope.DEFAULT, cancellation ?? new AsyncOperationInfo());
        }

        public override Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.TryReadStatusAsync(scope ?? DeviceOperationScope.DEFAULT, cancellation ?? new AsyncOperationInfo());
        }
    }
}
