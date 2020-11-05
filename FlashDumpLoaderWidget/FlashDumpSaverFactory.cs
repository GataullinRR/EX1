using DeviceBase.Devices;
using DeviceBase.IOModels;
using FlashDumpLoaderExports;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderWidget
{
    class FlashDumpSaverFactory : IFlashDumpSaver
    {
        public static IFlashDumpSaver Instance { get; }

        static FlashDumpSaverFactory()
        {
            Instance = new FlashDumpSaverFactory();
        }

        FlashDumpSaverFactory() { }

        public Task SaveAsync(RUSDeviceId deviceId,
                              IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> dataPacketFormats,
                              Stream rawDump,
                              Stream uncompressedParsedData,
                              Stream destination,
                              AsyncOperationInfo operationInfo)
        {
            return FlashDump.SaveAsync(deviceId, dataPacketFormats, rawDump, uncompressedParsedData, destination, operationInfo);
        }
    }
}