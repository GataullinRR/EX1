using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using Utilities.Extensions;
using Utilities.Types;

namespace Calibrators
{
    class RUSDeviceDataProviderProxy : RUSDeviceProxyBase, IDataProvider
    {
        public event DataRowAquiredDelegate DataRowAquired;

        public RUSDeviceDataProviderProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            var result = await _base.ReadAsync(request, scope, cancellation);
            if (request == Command.DATA &&
                result.Status == ReadStatus.OK)
            {
                DataRowAquired?.Invoke(result.Entities.GetViewEntities());
            }

            return result;
        }
    }
}
