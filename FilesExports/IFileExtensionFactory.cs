using DeviceBase;
using DeviceBase.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesExports
{
    public interface IFileExtensionFactory
    {
        IFileExtension GetExtension(RUSDeviceId deviceId, FileType fileType);
    }
}
