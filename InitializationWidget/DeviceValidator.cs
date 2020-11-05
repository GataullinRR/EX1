using Common;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InitializationWidget
{
    class DeviceValidator
    {
        readonly IRUSDevice _device;

        public DeviceValidator(IRUSDevice device)
        {
            _device = device;
        }

        public async Task<bool> CheckFilesAsync()
        {
            Logger.LogInfoEverywhere("Начата проверка файлов");

            using (Logger.Indent)
            {
                var serials = new List<object>();
                foreach (var fileRequest in _device.SupportedCommands.Where(r => r.IsFileRequest()))
                {
                    var fileType = fileRequest.GetFileType();
                    var result = await _device.ReadAsync(fileRequest, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (result.Status == ReadStatus.OK)
                    {
                        var actualSerial = Files.GetFileEntity(result.Entities, FileEntityType.SERIAL_NUMBER).Value;
                        if (Equals(actualSerial, Files.DEFAULT_SERIAL))
                        {
                            Logger.LogWarningEverywhere($"Файл \"{fileType.GetInfo().FileName}\" не установлен");

                            return false;
                        }

                        serials.Add(actualSerial);
                        if (serials.Any(s => !Equals(s, actualSerial)))
                        {
                            Logger.LogWarningEverywhere($"Серийные номера файлов не совпадают");

                            return false;
                        }
                    }
                    else
                    {
                        Logger.LogErrorEverywhere($"Не удалось прочитать файл \"{fileType.GetInfo().FileName}\"");

                        return false;
                    }
                }

                return true;
            }
        }
    }
}
