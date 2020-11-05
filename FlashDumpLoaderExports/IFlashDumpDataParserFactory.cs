using DeviceBase.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public interface IFlashDumpDataParserFactory
    {
        Task<IFlashDumpDataParser> CreateFromRawPartsAsync(IEnumerable<OpenStreamAsyncDelegate> rawPartsProvider, IDataPacketParser dataPacketParser, AsyncOperationInfo operationInfo);
    }
}
