using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.Devices
{
    public class RUSDeviceProxyBase : IRUSDevice
    {
        protected readonly IRUSDevice _base;

        public virtual RUSDeviceId Id => _base.Id;
        public virtual string Name => _base.Name;
        public virtual IReadOnlyList<Command> SupportedCommands => _base.SupportedCommands;
        public virtual IReadOnlyList<IRUSDevice> Children => _base.Children;

        public RUSDeviceProxyBase(IRUSDevice @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual Task ActivateDeviceAsync(DeviceOperationScope scope,AsyncOperationInfo cancellation)
        {
            return _base.ActivateDeviceAsync(scope, cancellation);
        }
        public virtual Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.DeactivateDeviceAsync(scope, cancellation);
        }

        public virtual Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.ReadAsync(request, scope, cancellation);
        }
        public virtual Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.BurnAsync(request, entities, scope, cancellation);
        }

        public virtual Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.TryReadStatusAsync(scope, cancellation);
        }

        public virtual T TryGetFeature<T>() where T : class, IRUSDeviceFeature
        {
            return _base.TryGetFeature<T>();
        }
    }
}
