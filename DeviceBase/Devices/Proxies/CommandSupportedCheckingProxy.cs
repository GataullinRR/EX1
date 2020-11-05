using Common;
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
    class CommandSupportedCheckingProxy : RUSDeviceProxyBase
    {
        readonly IRUSConnectionInterface _connectionInterface;

        public CommandSupportedCheckingProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (SupportedCommands.Contains(request))
            {
                return await _base.ReadAsync(request, scope, cancellation);
            }
            else
            {
                return new ReadResult(ReadStatus.NOT_SUPPORTED_BY_INTERFACE, Enumerable.Empty<IDataEntity>(), ResponseData.NONE);
            }
        }

        public override async Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (SupportedCommands.Contains(request))
            {
                return await _base.BurnAsync(request, entities, scope, cancellation);
            }
            else
            {
                return new BurnResult(BurnStatus.NOT_SUPPORTED_BY_INTERFACE, ResponseData.NONE);
            }
        }
    }
}
