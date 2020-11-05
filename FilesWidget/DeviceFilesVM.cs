using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using MVVMUtilities.Types;
using DeviceBase;
using System.Linq;
using Utilities.Extensions;
using System.IO;
using DeviceBase.Helpers;
using TinyConfig;
using DeviceBase.IOModels;
using DeviceBase.Models;
using DeviceBase.Devices;
using Common;
using WPFUtilities.Types;
using System.Runtime.Serialization;
using System.Threading;

namespace FilesWidget
{
    public class DeviceFilesVM
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("DeviceFilesVM");
        readonly static ConfigProxy<bool> WRITE_CURRENT_DATE_DEFAULT = CONFIG.Read(true);

        readonly IRUSDevice _device;
        public BusyObject IsBusy { get; }

        public IEnumerable<FileRequestVM> FileRequests { get; }

        public bool WriteCurrentDate
        {
            get => WRITE_CURRENT_DATE_DEFAULT;
            set => WRITE_CURRENT_DATE_DEFAULT.Value = value;
        }

        public DeviceFilesVM(IRUSDevice device, BusyObject isBusy)
        {
            _device = device;
            IsBusy = isBusy;

            FileRequests = new Enumerable<FileRequestVM>()
            {
                generateRequest("Калибровки", Command.CALIBRATION_FILE),
                generateRequest("Заводские настройки", Command.FACTORY_SETTINGS_FILE),
                generateRequest("Конфигурация пакета данных", Command.DATA_PACKET_CONFIGURATION_FILE),
                generateRequest("Температурные калибровки", Command.TEMPERATURE_CALIBRATION_FILE),
                generateRequest("Режим работы", Command.WORK_MODE_FILE),
            };

            FileRequestVM generateRequest(string name, Command request)
            {
                return new FileRequestVM(
                    name,
                    _device.SupportedCommands.Contains(request),
                    new ActionCommand(() => getFile(request), IsBusy),
                    new ActionCommand(() => burnFile(request), IsBusy)
                );
            }

            async Task getFile(Command request)
            {
                Logger.LogInfoEverywhere($"Чтение файла \"{request.GetFileType().GetInfo().FileName}\"");

                string settingsFile = null;
                using (IsBusy.BusyMode)
                {
                    var data = await _device.ReadAsync(request, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (data.Status == ReadStatus.OK)
                    {
                        settingsFile = new FileStringSerializer().Serialize(data.Entities);

                        Logger.LogOKEverywhere("Файл успешно прочитан");
                    }
                }

                if (settingsFile != null)
                {
                    var extensionInfo = new FileExtension(_device.Id, request.GetFileType());
                    var path = IOUtils.RequestFileSavingPath(extensionInfo.FileExtensionFilter);
                    if (path != null)
                    {
                        try
                        {
                            File.WriteAllText(path, settingsFile);
                        }
                        catch (Exception ex)
                        {
                            Reporter.ReportError("Ошибка в процессе сохранения", ex);
                            Logger.LogErrorEverywhere($"Не удалось сохранить файл на диск", ex);
                        }
                    }
                }
            }

            async Task burnFile(Command request)
            {
                Logger.LogInfoEverywhere($"Запись файла \"{request.GetFileType().GetInfo().FileName}\"");

                var ok = CommonUtils.Try(
                    () => getSerializedEntities()?.ToArray(),
                    out IEnumerable<IDataEntity> result,
                    out Exception exception);
                if (ok && result != null)
                {
                    result = WriteCurrentDate
                        ? Files.SetBurnDate(result, DateTimeOffset.Now.UtcDateTime)
                        : result;
                    using (IsBusy.BusyMode)
                    {
                        var burningResult = await _device.BurnAsync(request, result, DeviceOperationScope.DEFAULT, CancellationToken.None);
                        ok = (burningResult.Status == BurnStatus.OK);
                        if (!ok)
                        {
                            Logger.LogError(null, $"Не удалось выполнить запись. Запрос:{request}");
                            Reporter.ReportError("Не удалось выполнить запись.");
                        }
                    }
                }
                if (exception is SerializationException)
                {
                    Reporter.ReportError("Не удалось разобрать файл. Формат файла некорректен.", exception);
                    Logger.LogErrorEverywhere("Не удалось разобрать файл", exception);
                }
                else if (exception != null)
                {
                    Reporter.ReportError("Не удалось прочитать файл. Возможно он недоступен или имеет неверный формат.", exception);
                    Logger.LogErrorEverywhere("Не удалось прочитать файл", exception);
                }
                else if (result == null) // User closed the dialog
                {

                }

                IEnumerable<IDataEntity> getSerializedEntities()
                {
                    var serializer = new FileStringSerializer();
                    var extensionInfo = new FileExtension(_device.Id, request.GetFileType());
                    var filePath = IOUtils.RequestFileOpenPath(extensionInfo.FileExtensionFilter);
                    if (filePath == null)
                    {
                        return null;
                    }
                    else
                    {
                        var data = File.ReadAllText(filePath);
                        return serializer.Deserialize(data, request.GetFileType(), _device.Id);
                    }
                }
            }
        }
    }
}
