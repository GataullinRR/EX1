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
    class ErrorsCatchingProxy : RUSDeviceProxyBase
    {
        readonly IRUSConnectionInterface _connectionInterface;

        public ErrorsCatchingProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override async Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            try
            {
                await _base.ActivateDeviceAsync(scope, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка активации устройства", ex);
            }
        }
        public override async Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            try
            {
                await _base.DeactivateDeviceAsync(scope, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка деактивации устройства", ex);
            }
        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            try
            {
                return await _base.ReadAsync(request, scope, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка запроса чтения", ex);

                return new ReadResult(ReadStatus.UNKNOWN_ERROR, Enumerable.Empty<IDataEntity>(), ResponseData.NONE);
            }
        }
        public override async Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            try
            {
                return await _base.BurnAsync(request, entities, scope, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка запроса записи", ex);

                return new BurnResult(BurnStatus.UNKNOWN_ERROR, ResponseData.NONE);
            }
        }

        public override async Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            try
            {
                return await _base.TryReadStatusAsync(scope, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка чтения статуса", ex);

                return StatusReadResult.UNKNOWN_ERROR;
            }
        }
    }
}
