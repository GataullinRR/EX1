using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UtilitiesStandard.Types;

namespace DeviceBase.Devices
{
    public class RUSDeviceProxyBase : IRUSDevice
    {
        protected readonly IRUSDevice _base;

        public virtual byte Id => _base.Id;
        public virtual string Name => _base.Name;

        public virtual IReadOnlyList<RequestAddress> SupportedRequestAddresses => _base.SupportedRequestAddresses;
        public virtual IReadOnlyList<IRUSDevice> Children => throw new System.NotImplementedException();

        public RUSDeviceProxyBase(IRUSDevice @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual Task ActivateDeviceAsync(AsyncOperationInfo operationInfo = null)
            => _base.ActivateDeviceAsync(operationInfo);
        public virtual Task<BurnRequestResult> BurnAsync(RequestAddress request, IEnumerable<IDataEntity> entities, AsyncOperationInfo operationInfo = null)
            => _base.BurnAsync(request, entities, operationInfo);
        public virtual Task<bool> CheckIfConnected(AsyncOperationInfo operationInfo = null)
            => _base.CheckIfConnected(operationInfo);
        public virtual Task DeactivateDeviceAsync(AsyncOperationInfo operationInfo = null)
            => _base.DeactivateDeviceAsync(operationInfo);
        public virtual Task<ReadRequestResult> ReadAsync(RequestAddress request, AsyncOperationInfo operationInfo = null)
            => _base.ReadAsync(request, operationInfo);
        public virtual Task<DeviceStatusInfo> TryReadStatusAsync(AsyncOperationInfo operationInfo = null)
            => _base.TryReadStatusAsync(operationInfo);
    }
}
