using DeviceBase.Helpers;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class SynchronizationProxy : RUSDeviceProxyBase
    {
        readonly IRUSConnectionInterface _connectionInterface;

        public SynchronizationProxy(IRUSDevice @base, IRUSConnectionInterface connectionInterface) : base(@base) 
        {
            _connectionInterface = connectionInterface;
        }

        public override async Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            using (await _connectionInterface.LockAsync(cancellation))
            {
                await _base.ActivateDeviceAsync(scope, cancellation);
            }
        }
        public override async Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            using (await _connectionInterface.LockAsync(cancellation))
            {
                await _base.DeactivateDeviceAsync(scope, cancellation);
            }
        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            using (await _connectionInterface.LockAsync(cancellation))
            {
                return await _base.ReadAsync(request, scope, cancellation);
            }
        }
        public override async Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            using (await _connectionInterface.LockAsync(cancellation))
            {
                return await _base.BurnAsync(request, entities, scope, cancellation);
            }
        }

        public override async Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            using (await _connectionInterface.LockAsync(cancellation))
            {
                return await _base.TryReadStatusAsync(scope, cancellation);
            }
        }
    }
}
