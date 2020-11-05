using Common;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    abstract class RUSFTDIDeviceBase : RUSDeviceBase
    {
        FTDIBoxFeatures _ftdiInterface;

        public RUSFTDIDeviceBase(MiddlewaredConnectionInterfaceDecorator pipe) : base(pipe)
        {
        }
        public RUSFTDIDeviceBase(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children) : base(pipe, children)
        {
        }

        public override sealed async Task<ReadResult> ReadAsync(Command requestAddress, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (!await tryInitializeFTDIInterfaceIfRequiredAsync(scope, cancellation))
            {
                return new ReadResult(ReadStatus.COULD_NOT_INITIALIZE, null, null);
            }

            if (requestAddress == Command.DOWNLOAD_FLASH)
            {
                var modeSetResult = await BurnAsync(Command.SET_DATA_UNLOAD_MODE, new IDataEntity[0], scope, cancellation);
                var file = modeSetResult.Status == BurnStatus.OK
                    ? await _ftdiInterface.ReadFileAsync(scope, cancellation)
                    : null;
                var modeUnsetResult = modeSetResult.Status == BurnStatus.OK
                    ? await BurnAsync(Command.SET_FLASH_WORK_MODE, new IDataEntity[0], scope, cancellation)
                    : null;

                if (modeSetResult.Status != BurnStatus.OK)
                {
                    Logger.LogErrorEverywhere("Не удалось перевести флеш в режим выгрузки");

                    return new ReadResult(ReadStatus.COULD_NOT_INITIALIZE, null, null);
                }
                if (file == null)
                {
                    Logger.LogErrorEverywhere("Не удалось считать данные из флеш");

                    return new ReadResult(ReadStatus.REQUEST_ERROR, null, null);
                }
                if (modeUnsetResult.Status != BurnStatus.OK)
                {
                    Logger.LogErrorEverywhere("Не удалось перевести флеш в режим работы");

                    return new ReadResult(ReadStatus.OK, null, file);
                }

                if (file != null)
                {
                    return new ReadResult(ReadStatus.OK, null, file);
                }
                else
                {
                    return new ReadResult(ReadStatus.REQUEST_ERROR, null, file);
                }
            }
            else
            {
                if (!await trySetCOMPortModeAsync(scope, cancellation))
                {
                    return new ReadResult(ReadStatus.COULD_NOT_INITIALIZE, null, null);
                }
                else
                {
                    return await base.ReadAsync(requestAddress, scope, cancellation);
                }
            }
        }
        protected override async Task<BurnResult> burnAsync(Command requestAddress, IEnumerable<IDataEntity> entities, bool broadcast, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            if (!await trySetCOMPortModeAsync(scope, cancellation))
            {
                return new BurnResult(BurnStatus.COULD_NOT_INITIALIZE, null);
            }

            return await base.burnAsync(requestAddress, entities, broadcast, scope, cancellation);
        }

        protected override async Task activateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await base.activateDeviceAsync(scope, cancellation);

            await tryInitializeFTDIInterfaceIfRequiredAsync(scope, cancellation);
        }

        protected override async Task deactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await base.deactivateDeviceAsync(scope, cancellation);

            _ftdiInterface = null;
        }

        async Task<bool> tryInitializeFTDIInterfaceIfRequiredAsync(DeviceOperationScope scope, CancellationToken cancellation)
        {
            if (_pipe.InterfaceDevice == InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX)
            {
                if (_ftdiInterface == null)
                {
                    _ftdiInterface = await FTDIBoxFeatures.CreateAsync(_pipe, scope, cancellation);
                }

                if (_ftdiInterface == null)
                {
                    Logger.LogErrorEverywhere("Не удалось инициализировать");
                }

                return _ftdiInterface != null;
            }
            else
            {
                return true;
            }
        }

        async Task<bool> trySetCOMPortModeAsync(DeviceOperationScope scope, CancellationToken cancellation)
        {
            if (_pipe.InterfaceDevice == InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX)
            {
                if (!await tryInitializeFTDIInterfaceIfRequiredAsync(scope, cancellation))
                {
                    return false;
                }
                else if (!await _ftdiInterface.TrySwitchToRegularModeAsync(scope, cancellation))
                {
                    Logger.LogErrorEverywhere("Не удалось установить режим передачи");

                    return false;
                }
            }

            return true;
        }
    }
}
