using Common;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.Models;
using FilesExports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace CalibrationWidget
{
    public class FileSaver
    {
        readonly RUSDeviceId _deviceId;
        readonly IFileExtensionFactory _extensionFactory;

        public FileSaver(RUSDeviceId deviceId, IFileExtensionFactory extensionFactory)
        {
            _deviceId = deviceId;
            _extensionFactory = extensionFactory;
        }

        public async Task SaveCalibrationFileAsync(FileType fileType, IEnumerable<IDataEntity> dataEntities)
        {
            await ThreadingUtils.ContinueAtThreadPull();

            var serialized = new FileStringSerializer().Serialize(dataEntities);
            if (serialized != null)
            {
                var extensionInfo = _extensionFactory.GetExtension(_deviceId, fileType);
                var path = IOUtils.RequestFileSavingPath(extensionInfo.FileExtensionFilter);
                if (path != null)
                {
                    try
                    {
                        File.WriteAllText(path, serialized);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere($"Не удалось сохранить файл на диск", ex);
                    }
                }
            }
        }
    }
}
