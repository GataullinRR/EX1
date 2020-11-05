using DataViewExports;
using System.IO;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public interface IFlashDumpDataParser
    {
        Task<Stream> GetParsedDataStreamAsync(AsyncOperationInfo operationInfo);
        Task<IRowsReader> InstantiateReaderAsync(AsyncOperationInfo operationInfo);
    }
}
