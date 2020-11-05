using System.Threading.Tasks;
using Utilities.Types;
using System.IO;
using System.Threading;
using Utilities.Extensions;

namespace FlashUploadWidget
{
    class StreamPartsProviderSupplier
    {
        readonly Stream _flashDumpWriteStream;
        readonly DumpParserPartsProvider _partsProvider;
        readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
        long _lastPosition = 0;

        public StreamPartsProviderSupplier(FillNotifiableWriteOnlyStreamDecorator flashDumpWriteStream, DumpParserPartsProvider partsProvider)
        {
            _flashDumpWriteStream = flashDumpWriteStream;
            _partsProvider = partsProvider;

            flashDumpWriteStream.LengthChangedAsync += FlashDumpWriteStream_SizeChangedAsync;
        }

        public async Task ProvideLastPartAsync()
        {
            await provideNextPartAsync();
            _partsProvider.Complete();
        }

        async Task provideNextPartAsync()
        {
            using (await _locker.AcquireAsync())
            {
                var length = _flashDumpWriteStream.Length - _lastPosition;
                await _partsProvider.ProvideNextAndWaitTillFinishesAsync(_lastPosition, length, new AsyncOperationInfo());
                _lastPosition += length;
            }
        }

        async void FlashDumpWriteStream_SizeChangedAsync()
        {
            await provideNextPartAsync();
        }
    }
}
