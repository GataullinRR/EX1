using Common;
using DeviceBase.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;
using System.IO;
using DeviceBase.Models;
using FlashDumpLoaderExports;

namespace FlashUploadWidget
{
    class FlashStreamReader
    {
        const int CHUNK_SIZE = 20 * 1024 * 1024;

        public DeviceOperationScope FlashReadOperationScope { get; }

        readonly RUSDeviceId _device;
        readonly IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> _rowDescriptors;
        readonly Task<IFlashDumpDataParser> _parserFuture;
        readonly string _rawDumpPath;
        readonly StreamPartsProviderSupplier _partsProviderSupplier;
        readonly Stream _rawDumpStream;
        IFlashDumpDataParser _parsingResult;

        FlashStreamReader(
            DeviceOperationScope flashReadOperationScope,
            RUSDeviceId device,
            IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> rowDescriptors,
            Task<IFlashDumpDataParser> parserFuture,
            StreamPartsProviderSupplier partsProviderSupplier,
            Stream rawDumpStream,
            string rawDumpPath)
        {
            FlashReadOperationScope = flashReadOperationScope ?? throw new ArgumentNullException(nameof(flashReadOperationScope));
            _device = device;
            _rowDescriptors = rowDescriptors ?? throw new ArgumentNullException(nameof(rowDescriptors));
            _parserFuture = parserFuture ?? throw new ArgumentNullException(nameof(parserFuture));
            _partsProviderSupplier = partsProviderSupplier ?? throw new ArgumentNullException(nameof(partsProviderSupplier));
            _rawDumpStream = rawDumpStream;
            _rawDumpPath = rawDumpPath ?? throw new ArgumentNullException(nameof(rawDumpPath));
        }

        public async static Task<FlashStreamReader> CreateAsync(RUSDeviceId device,
            IDictionary<RUSDeviceId, IEnumerable<IDataEntity>> formats,
            IFlashDumpDataParserFactory parserFactory,
            AsyncOperationInfo operationInfo)
        {
            var flashDumpPath = Storaging.GetTempFilePath();
            var baseStream = new FileStream(flashDumpPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            var flashDumpWriteStream = new FillNotifiableWriteOnlyStreamDecorator(CHUNK_SIZE, baseStream);
            var partsProvider = new DumpParserPartsProvider(flashDumpPath);
            var partsProviderSupplier = new StreamPartsProviderSupplier(flashDumpWriteStream, partsProvider);
            var scope = new DeviceOperationScope(new FlashDumpStreamParameter(flashDumpWriteStream));
            var sections = formats.Select(f =>
                new SectionedDataPacketParser.Section(
                    f.Key,
                    new DataPacketParser(EntitiesDeserializer.ExtractDataPacketDescriptors(f.Value))))
                .ToArray();
            var dataParser = new SectionedDataPacketParser(sections);
            var parserFuture = parserFactory.CreateFromRawPartsAsync(partsProvider.RawDumpParts(), dataParser, operationInfo);

            return new FlashStreamReader(scope, device, formats, parserFuture, partsProviderSupplier, flashDumpWriteStream, flashDumpPath);
        }

        public async Task FinishAsync(AsyncOperationInfo operationInfo)
        {
            await _partsProviderSupplier.ProvideLastPartAsync();// Provide last piece of data
            _parsingResult = await _parserFuture;
            _rawDumpStream.Dispose();
        }

        public async Task SaveDumpAsync(Stream destination, IFlashDumpSaver flashDumpSaver, AsyncOperationInfo operationInfo)
        {
            var parsedRows = await _parsingResult.GetParsedDataStreamAsync(operationInfo);
            var rawDump = new FileStream(_rawDumpPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await flashDumpSaver.SaveAsync(_device, _rowDescriptors, rawDump, parsedRows, destination, operationInfo);
        }
    }
}
