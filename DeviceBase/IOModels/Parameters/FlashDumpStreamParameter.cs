using System.IO;

namespace DeviceBase.IOModels
{
    /// <summary>
    /// Parameter to <see cref="DeviceBase.Devices.Command.FLASH_DATA"/>
    /// </summary>
    public class FlashDumpStreamParameter : IRequestParameter
    {
        /// <summary>
        /// The stream where Flash dump will be written
        /// </summary>
        public Stream Stream { get; }

        public FlashDumpStreamParameter(Stream stream)
        {
            Stream = stream;
        }
    }
}
