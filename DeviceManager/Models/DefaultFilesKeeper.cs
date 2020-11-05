using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase;
using System.IO;
using DeviceBase.Models;
using Common;
using FilesExports;

namespace RUSManagingTool.Models
{
    class DefaultFilesKeeper
    {
        public void CreateAllFiles(string directory, IFileExtensionFactory extensionFactory)
        {
            IOUtils.TryDeleteAllFilesInDirectory(directory);

            var serializer = new FileStringSerializer();
            foreach (var file in Files.Descriptors)
            {
                var extensionInfo = extensionFactory.GetExtension(file.Key.TargetDeviceId, file.Key.FileType);
                var fileName = $"V{file.Key.FileFormatVersion}{extensionInfo.Extension}";
                var path = Path.Combine(directory, fileName);
                var serialized = serializer.Serialize(file.Value.Descriptors.Select(d => d.FileDefaultDataEntity));
                try
                {
                    using (var stream = new StreamWriter(IOUtils.TryCreateFileOrNull(path)))
                    {
                        stream.Write(serialized);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(null, $"Не удалось создать или сохранить файл по пути: {path}", ex);
                }
            }
        }
    }
}
