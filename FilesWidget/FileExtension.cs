using DeviceBase;
using DeviceBase.Devices;
using FilesExports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace FilesWidget
{
    class FileExtension : IFileExtension
    {
        public string Extension { get; }
        public string FileExtensionFilter { get; }

        public FileExtension(RUSDeviceId deviceId, FileType fileType)
        {
            var fileTypeString = fileType.ToString(ft => ft.ToString(),
                (FileType.CALIBRATION, "Cal"),
                (FileType.DATA_PACKET_CONFIGURATION, "DPConf"),
                (FileType.FACTORY_SETTINGS, "FSet"),
                (FileType.TEMPERATURE_CALIBRATION, "TCal"),
                (FileType.WORK_MODE, "WMode"));

            Extension = $".RUS{((byte)deviceId).ToString("D2")}-{fileTypeString}.txt";
            FileExtensionFilter = $"Файл устройства (*{Extension})|*{Extension}";
        }
    }
}
