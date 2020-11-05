using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class FTDIBoxFeatures
    {
        enum GetFirstAvailableBlockStatus : byte
        {
            ERROR = 0,
            IS_FULL = 1,
            IS_EMPTY = 2,
            IS_PARTIALLY_FILLED = 3,
        }

        enum FlashReadStatus : byte
        {
            OK = 0,
            ERROR = 1,
        }

        public enum Mode
        {
            /// <summary>
            /// When mode is not surely known
            /// </summary>
            UNKNOWN,
            COM_PORT,
            HIGH_SPEED
        }

        const int CHANNEL_ACTIVATION_DELAY = 0;
        const long MAX_FLASH_SIZE = 4 * 1024 * 1024;
        const int FLASH_BLOCK_SIZE = 2 * 1024;
        static FTDIBoxRequest instantiate_LS_SELECT_CHANNEL4(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.LS_ACTIVATE_CHANNEL4, scope);
        static FTDIBoxRequest instantiate_HS_SELECT_CHANNEL4(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.HS_ACTIVATE_CHANNEL4, scope);
        static FTDIBoxRequest instantiate_HS_SET_LOW_SPEED_MODE(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.HS_SET_LOW_SPEED_MODE, scope);
        static FTDIBoxRequest instantiate_LS_SET_HIGH_SPEED_MODE(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.LS_SET_HIGH_SPEED_MODE, scope);
        static  FTDIBoxRequest instantiate_HS_GET_FIRST_AVAILABLE_FLASH_BLOCK(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.HS_GET_FLASH_LENGTH, scope);
        static  FTDIBoxRequest instantiate_HS_READ_ALL_FLASH(DeviceOperationScope scope) 
            => FTDIBoxRequest.CreateCommandRequest(FTDIBoxRequestAddress.HS_READ_ALL_FLASH, scope);

        readonly IRUSConnectionInterface _pipe;
        Mode _currentMode;

        FTDIBoxFeatures(IRUSConnectionInterface pipe)
        {
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        }

        public static async Task<FTDIBoxFeatures> CreateAsync(IRUSConnectionInterface pipe, DeviceOperationScope scope, CancellationToken cancellation)
        {
            var device = new FTDIBoxFeatures(pipe);
            var ok = await device.trySetModeAsync(Mode.HIGH_SPEED, scope, cancellation);
            ok |= await device.trySetModeAsync(Mode.COM_PORT, scope, cancellation);
            if (!ok)
            {
                Logger.LogErrorEverywhere("Ошибка инициализации устройства");

                device = null;
            }

            return device;
        }

        public async Task<bool> TrySwitchToRegularModeAsync(DeviceOperationScope scope, CancellationToken cancellation)
        {
            return await trySetModeAsync(Mode.COM_PORT, scope, cancellation);
        }

        /// <summary>
        /// Very long operation, returns huge file
        /// </summary>
        /// <param name="asyncOperation"></param>
        /// <returns></returns>
        public async Task<IResponseData> ReadFileAsync(DeviceOperationScope scope, AsyncOperationInfo asyncOperation)
        {
            using (Logger.Indent)
            {
                Logger.LogInfo(null, $"Чтение дампа флеш...");

                IResponseData result = null;
                var ok = await trySetModeAsync(Mode.HIGH_SPEED, scope, asyncOperation.CancellationToken);
                if (ok)
                {
                    var estimatedFlashLength = (await tryGetFlashLengthAsync()) ?? MAX_FLASH_SIZE; // Estimated (not exact) length of the file to be transfered
                    asyncOperation.Progress.MaxProgress = estimatedFlashLength;
                    var response = await _pipe.RequestAsync(instantiate_HS_READ_ALL_FLASH(scope), scope, asyncOperation).ThenDo(r => r.To<FTDIBoxResponse>());
                    if (response.Status == RequestStatus.OK)
                    {
                        var serviceData = await response.ServiceData.GetAllAsync(asyncOperation);
                        var status = EnumUtils.TryCastOrNull<FlashReadStatus>(serviceData[5]);
                        if (status == FlashReadStatus.OK)
                        {
                            Logger.LogOKEverywhere("Файл успешно прочитан");
                        }
                        else
                        {
                            Logger.LogError("Ошибка чтения файла", $"-MSG. Сервис данные: {serviceData.ToHex()}");
                        }

                        result = response.Body;
                        asyncOperation.Progress.Report(asyncOperation.Progress.MaxProgress);
                    }
                }

                return result;
            }

            async Task<long?> tryGetFlashLengthAsync()
            {
                using (Logger.Indent)
                {
                    Logger.LogInfo(null, $"Чтение длины флеш");

                    var result = await _pipe.RequestAsync(instantiate_HS_GET_FIRST_AVAILABLE_FLASH_BLOCK(scope), scope, asyncOperation.CancellationToken);
                    if (result.Status == RequestStatus.OK)
                    {
                        var iterator = result.Data.StartEnumeration();
                        iterator.Pull(2);
                        var lengthInBytes = iterator.Pull(3).Concat(0.ToByte()).DeserializeInt32(false) * (long)FLASH_BLOCK_SIZE;
                        var status = EnumUtils.TryCastOrNull<GetFirstAvailableBlockStatus>(iterator.AdvanceOrThrow());
                        switch (status)
                        {
                            case null:
                            case GetFirstAvailableBlockStatus.ERROR:
                                Logger.LogErrorEverywhere("Ошибка получения длины флеш");
                                return null;

                            case GetFirstAvailableBlockStatus.IS_FULL:
                            case GetFirstAvailableBlockStatus.IS_EMPTY:
                            case GetFirstAvailableBlockStatus.IS_PARTIALLY_FILLED:
                                Logger.LogOK(null, $"Длина флеш: {lengthInBytes}");
                                return lengthInBytes;

                            default:
                                throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        async Task<bool> trySetModeAsync(Mode mode, DeviceOperationScope scope, CancellationToken cancellation)
        {
            Logger.LogInfo(null, $"Установка режима: {mode}, текуший режим: {_currentMode}");

            if (_currentMode != mode)
            {
                var ok = true;
                switch (mode)
                {
                    case Mode.UNKNOWN:
                        throw new ArgumentOutOfRangeException();
                    case Mode.COM_PORT:
                        ok &= await _pipe.RequestAsync(instantiate_HS_SET_LOW_SPEED_MODE(scope), scope, cancellation).ThenDo(r => r.Status == RequestStatus.OK);
                        ok &= await _pipe.RequestAsync(instantiate_LS_SELECT_CHANNEL4(scope), scope, cancellation).ThenDo(r => r.Status == RequestStatus.OK);
                        break;
                    case Mode.HIGH_SPEED:
                        ok &= await _pipe.RequestAsync(instantiate_LS_SET_HIGH_SPEED_MODE(scope), scope, cancellation).ThenDo(r => r.Status == RequestStatus.OK);
                        ok &= await _pipe.RequestAsync(instantiate_HS_SELECT_CHANNEL4(scope), scope, cancellation).ThenDo(r => r.Status == RequestStatus.OK);
                        break;

                    default:
                        throw new NotSupportedException();
                }
                await Task.Delay(CHANNEL_ACTIVATION_DELAY);

                if (ok)
                {
                    _currentMode = mode;
                }
                else
                {
                    _currentMode = Mode.UNKNOWN;
                }
            }

            return _currentMode == mode;
        }
    }
}
