using Common;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    abstract class RUSModuleBase : RUSFTDIDeviceBase
    {
        const int CLOCK_SYNC_INTERVAL = 5000;
        const bool CLOCK_SYNC_ENABLED = true;

        (AsyncOperationInfo Operation, Task Future)? _clockSyncLoopInfo;

        public RUSModuleBase(MiddlewaredConnectionInterfaceDecorator pipe) : this(pipe, new IRUSDevice[0])
        {

        }

        public RUSModuleBase(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children) : base(pipe, children)
        {

        }

        protected override sealed async Task activateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (CLOCK_SYNC_ENABLED)
            {
                var opInfo = new AsyncOperationInfo().UseInternalCancellationSource();
                var future = clockSyncLoop(opInfo);

                _clockSyncLoopInfo = (opInfo, future);
            }

            await base.activateDeviceAsync(scope, cancellation);
        }
        protected override sealed async Task deactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (CLOCK_SYNC_ENABLED)
            {
                _clockSyncLoopInfo.Value.Operation.Cancel();
                await _clockSyncLoopInfo.Value.Future.CatchOperationCanceledExeption();
                _clockSyncLoopInfo = null;
            }

            await base.deactivateDeviceAsync(scope, cancellation);
        }

        async Task clockSyncLoop(AsyncOperationInfo asyncOperation)
        {
            Logger.LogInfo(null, "Цикл синхронизации часов запущен");

            var period = new PeriodDelay(CLOCK_SYNC_INTERVAL);
            while (true)
            {
                try
                {
                    using (await _pipe.LockAsync(asyncOperation.CancellationToken))
                    {
                        var body = Requests
                            .GetRequestDescription(0, Command.CLOCKS_SYNC)
                            .BuildWriteRequestBody(new IDataEntity[0]);
                        var response = await burnAsync(Command.CLOCKS_SYNC, body, true, DeviceOperationScope.DEFAULT, asyncOperation.CancellationToken);
                        if (response.Status != BurnStatus.OK)
                        {
                            Logger.LogErrorEverywhere("Не удалось синхронизировать часы");
                        }
                    }

                    await period.WaitTimeLeftAsync(asyncOperation.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogErrorEverywhere("Ошибка синхронизации часов", ex);
                }
            }
        }
    }
}
