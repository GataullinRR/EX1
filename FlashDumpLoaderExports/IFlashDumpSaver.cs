using DeviceBase.Devices;
using DeviceBase.IOModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public interface IFlashDumpSaver
    {
        Task SaveAsync(RUSDeviceId deviceId,
            IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> dataPacketFormats,
            Stream rawDump,
            Stream uncompressedParsedData,
            Stream destination,
            AsyncOperationInfo operationInfo);
    }
}
