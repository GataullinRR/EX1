using DeviceBase.Models;
using FlashDumpLoaderExports;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderWidget
{
    class FlashDumpDataParserFactory : IFlashDumpDataParserFactory
    {
        public static IFlashDumpDataParserFactory Instance { get; }

        static FlashDumpDataParserFactory()
        {
            Instance = new FlashDumpDataParserFactory();
        }

        FlashDumpDataParserFactory() { }

        public async Task<IFlashDumpDataParser> CreateFromRawPartsAsync(IEnumerable<OpenStreamAsyncDelegate> rawPartsProvider, IDataPacketParser dataPacketParser, AsyncOperationInfo operationInfo)
        {
            return await FlashDumpDataParser.CreateParserAsync(rawPartsProvider, dataPacketParser, operationInfo);
        }
    }
}