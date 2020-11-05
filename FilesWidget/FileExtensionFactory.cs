using DeviceBase;
using DeviceBase.Devices;
using FilesExports;

namespace FilesWidget
{
    public class FileExtensionFactory : IFileExtensionFactory
    {
        public static IFileExtensionFactory Instance { get; }

        static FileExtensionFactory()
        {
            Instance = new FileExtensionFactory();
        }
        FileExtensionFactory() { }

        public IFileExtension GetExtension(RUSDeviceId deviceId, FileType fileType)
        {
            return new FileExtension(deviceId, fileType);
        }
    }
}
