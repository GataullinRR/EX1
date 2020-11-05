using Common;
using DataViewExports;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using DeviceBase.Models;
using FlashDumpLoaderExports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace FlashDumpLoaderWidget
{
    class FlashDumpDataParser : IFlashDumpDataParser
    {
        static readonly int INDEXER_DEGREE_OF_PARALLELISM = Environment.ProcessorCount * 2;
        static readonly int PARSER_DEGREE_OF_PARALLELISM = Environment.ProcessorCount * 2;
        /// <summary>
        /// Header of the request (we store rows inside flash as Salachov data request)
        /// </summary>
        const int ROW_DATA_START_OFFSET = 2 + 2 + 2; // command + reserved + length
        /// <summary>
        /// CRC16
        /// </summary>
        const int ROW_DATA_END_OFFSET = 2; // CRC

        public delegate Task<Stream> StreamAsyncFactoryDelegate(int readBufferSize);

        static readonly int MAX_ROW_LENGTH = ushort.MaxValue + ROW_DATA_START_OFFSET + ROW_DATA_END_OFFSET;
        static readonly byte[] ROW_START_MARKER = new byte[] { 0x01, 0x00, 0x0E, 0x05, 0x05, 0x57, 0xD7, 0xFD };
        static readonly int READ_BUFFER_LENGTH = (MAX_ROW_LENGTH + ROW_START_MARKER.Length) * 3; // Best size. Chosen empirically

        readonly Func<AsyncOperationInfo, Task<Stream>> _parsedDataStreamFactory;
        readonly int _numOfCurves;
        readonly int _rowsCount;

        FlashDumpDataParser(int numOfCurves, int rowsCount, Func<AsyncOperationInfo, Task<Stream>> parsedDataStreamFactory)
        {
            _numOfCurves = numOfCurves;
            _rowsCount = rowsCount;
            _parsedDataStreamFactory = parsedDataStreamFactory;
        }

        public Task<Stream> GetParsedDataStreamAsync(AsyncOperationInfo operationInfo)
        {
            return _parsedDataStreamFactory(operationInfo);
        }

        public async Task<IRowsReader> InstantiateReaderAsync(AsyncOperationInfo operationInfo)
        {
            return new FlashDumpRowsReader(_numOfCurves, _rowsCount, () => _parsedDataStreamFactory(new AsyncOperationInfo()));
        }

        /// <summary>
        /// Parses data and saves them to <see cref="FlashDump"/> (if <see cref="FlashDump.HasParsedData"/> set to <see cref="false"/>)
        /// </summary>
        /// <param name="dump"></param>
        /// <param name="rowParser"></param>
        /// <param name="operationInfo"></param>
        /// <returns></returns>
        public static async Task<FlashDumpDataParser> CreateParserAsync(FlashDump dump, IDataPacketParser rowParser, AsyncOperationInfo operationInfo)
        {
            if (dump.HasParsedData)
            {
                using (var stream = await dump.OpenParsedDataStreamAsync(new StreamParameters(1000), operationInfo))
                {
                    var rowsCount = stream.Length / (rowParser.Curves.Length * sizeof(double));

                    Logger.LogOKEverywhere($"Дамп загружен-NLКоличество строк: {rowsCount}");

                    return new FlashDumpDataParser(rowParser.Curves.Length, (int)rowsCount, oi => dump.OpenParsedDataStreamAsync(new StreamParameters(READ_BUFFER_LENGTH), oi));
                }
            }
            else
            {
                return await CreateParserAsync(new OpenStreamAsyncDelegate[] { dump.OpenRawDataStreamAsync }, rowParser, operationInfo);
            }
        }

        public static async Task<FlashDumpDataParser> CreateParserAsync(IEnumerable<OpenStreamAsyncDelegate> rawDumpParts, IDataPacketParser rowParser, AsyncOperationInfo operationInfo)
        {
            Logger.LogInfoEverywhere("Начат парсинг дампа Flash памяти");
            var sw = Stopwatch.StartNew();
            await ThreadingUtils.ContinueAtDedicatedThread(operationInfo);

            var resultsStreamPath = Storaging.GetTempFilePath();
            int parsedRowsCount = 0; //Accessed inside multiple threads!
            int skippedRowsCount = 0; //Accessed inside multiple threads!
            var dumpLengthInMegabytes = 0D;
            using (var resultsStream = new FileStream(resultsStreamPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                foreach (var part in rawDumpParts)
                {
                    var partLength = await getDumpLengthInMegabytesAsync(part);
                    Logger.LogInfo(null, $"Парсинг части дампа длиной: {partLength:F2} Мб");
                    dumpLengthInMegabytes += partLength;

                    var allRowsStartIndexes = await getRowsIndexes(part);
                    await parseRowsAsync(allRowsStartIndexes, part, resultsStream);
                }
            }

            Logger.LogOK($"Парсинг дампа завершен-NLСтрок считано: {parsedRowsCount}-NLСтрок пропущено: {skippedRowsCount}", $"-MSG-NLСредняя скорость чтения: {dumpLengthInMegabytes / sw.Elapsed.TotalSeconds:F1} Мб/с");

            return new FlashDumpDataParser(
                rowParser.Curves.Length, 
                parsedRowsCount, 
                oi => Task.Run(() => (Stream)new FileStream(resultsStreamPath,  FileMode.Open, FileAccess.Read, FileShare.Read)));

            async Task<List<long>> getRowsIndexes(OpenStreamAsyncDelegate streamsFactory)
            {
                using (var mainStream = await streamsFactory(new StreamParameters(READ_BUFFER_LENGTH), operationInfo))
                {
                    var indexerEndPositions = new long[INDEXER_DEGREE_OF_PARALLELISM];
                    var indexerChunkSize = mainStream.Length / INDEXER_DEGREE_OF_PARALLELISM;
                    Logger.LogInfo(null, "Разбиение файла перед индексацией...");
                    for (int i = 0; i < INDEXER_DEGREE_OF_PARALLELISM - 1; i++)
                    {
                        mainStream.Position = indexerChunkSize * (i + 1);
                        var beginningOfTheMarker = findAllRowIndexes(mainStream, 0).FirstOrDefault(-1);
                        if (beginningOfTheMarker == -1)
                        {
                            Logger.LogWarning(null, $"Не удалось найти начало строки после позиции: {mainStream.Position}. Данные после данной позиции (если есть) будут проигнорированы");

                            break;
                        }
                        else
                        {
                            indexerEndPositions[i] = beginningOfTheMarker;
                        }
                        operationInfo.CancellationToken.ThrowIfCancellationRequested();
                    }
                    indexerEndPositions[INDEXER_DEGREE_OF_PARALLELISM - 1] = mainStream.Length;

                    Logger.LogInfo(null, "Индексация...");
                    var indexersFuture = new Task<long[]>[INDEXER_DEGREE_OF_PARALLELISM];
                    var range = new DisplaceCollection<long>(2);
                    range.Add(0);
                    for (int i = 0; i < indexerEndPositions.Length; i++)
                    {
                        range.Add(indexerEndPositions[i]);

                        var from = range.FirstElement();
                        var to = range.LastElement();
                        var rootStream = await streamsFactory(new StreamParameters(READ_BUFFER_LENGTH), operationInfo);
                        rootStream.Position = from;
                        var section = new SectionedStreamProxy(rootStream, to - from);
                        indexersFuture[i] = findAllRowIndexesAsync(section, from);

                        Logger.LogInfo(null, $"Запущен поток индексации в интервале: {from} : {to}");
                    }

                    var result = new List<long>((mainStream.Length / 200).ToInt32()); // Estimated size (dont want list to increase it's buffer too much)
                    foreach (var future in indexersFuture)
                    {
                        try
                        {
                            var indexes = await future;
                            result.AddRange(indexes);
                        }
                        catch (OperationCanceledException) // Cant throw here 
                        {
                            Logger.LogInfo(null, "Чтение дампа отменено");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(null, "Ошибка индексации. Большой объем данных может быть потерян", ex);
                        }
                    }
                    
                    operationInfo.CancellationToken.ThrowIfCancellationRequested();
                    Logger.LogInfo(null, $"Индексация завершена. Найдено строк: {result.Count}");

                    return result;
                }

                async Task<long[]> findAllRowIndexesAsync(Stream section, long from)
                {
                    await ThreadingUtils.ContinueAtDedicatedThread(operationInfo);

                    return findAllRowIndexes(section, from).ToArray();
                }
            }

            async Task parseRowsAsync(IList<long> allRowsStartIndexes, OpenStreamAsyncDelegate rawDataStreamAsyncFactory, Stream resultDestinationStream)
            {
                var rowsIndexes = getRowsIndexesForParsing();
                var parsersFuture = new Task<Stream>[rowsIndexes.Length];
                for (int i = 0; i < rowsIndexes.Length; i++)
                {
                    var indexes = rowsIndexes[i];
                    parsersFuture[i] = parseRowsRangeAsync(indexes);
                }

                foreach (var future in parsersFuture)
                {
                    Stream parsedRowsStream = null;
                    try
                    {
                        using (parsedRowsStream = await future)
                        {
                            parsedRowsStream.Position = 0;
                            await parsedRowsStream.CopyToAsync(resultDestinationStream, 81920, operationInfo);
                            parsedRowsStream.SetLength(0); // Delete file
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogInfo(null, "Чтение дампа отменено");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(null, "Ошибка индексации. Большой объем данных может быть потерян", ex);
                    }
                    finally
                    {
                        parsedRowsStream?.Dispose();
                    }
                }
                operationInfo.CancellationToken.ThrowIfCancellationRequested();

                IEnumerable<long>[] getRowsIndexesForParsing()
                {
                    var chunks = new IEnumerable<long>[PARSER_DEGREE_OF_PARALLELISM];
                    var chunkRange = new DisplaceCollection<int>(2);
                    chunkRange.Add(0);
                    var chunkSize = allRowsStartIndexes.Count / PARSER_DEGREE_OF_PARALLELISM;
                    if (chunkSize == 0)
                    {
                        Logger.LogError(null, "Слишком мало данных для обработки");

                        chunks.SetAll(new long[0]);
                    }
                    for (int i = 0; i < PARSER_DEGREE_OF_PARALLELISM; i++)
                    {
                        chunkRange.Add((i + 1) * chunkSize);
                        var from = chunkRange.FirstElement();
                        from = from == 0
                            ? from
                            : from - 1; // We should create overlap for 1 element, otherwise one row will be lost
                        var to = chunkRange.LastElement();
                        chunks[i] = allRowsStartIndexes.GetRangeTill(from, to);
                    }

                    return chunks;
                }

                async Task<Stream> parseRowsRangeAsync(IEnumerable<long> rowsStarts)
                {
                    await ThreadingUtils.ContinueAtDedicatedThread(operationInfo);

                    using (var sourceFile = await rawDataStreamAsyncFactory(new StreamParameters(READ_BUFFER_LENGTH), operationInfo))
                    {
                        var resultFile = getTempFileStream().ToBinaryWriter();
                        var rowPositionRange = new DisplaceCollection<long>(2);
                        rowPositionRange.Add(rowsStarts.FirstOrDefault());
                        var rowBuffer = new byte[rowParser.RowLength.To];
                        foreach (var rowStart in rowsStarts.Skip(1))
                        {
                            operationInfo.CancellationToken.ThrowIfCancellationRequested();
                            rowPositionRange.Add(rowStart);
                            var rowDataAreaStart = rowPositionRange.FirstElement() + ROW_START_MARKER.Length + ROW_DATA_START_OFFSET;
                            var rowDataAreaEnd = rowPositionRange.LastElement() - ROW_DATA_END_OFFSET;
                            var actualRowLength = rowDataAreaEnd - rowDataAreaStart;
                            if (actualRowLength < rowParser.RowLength.From)
                            {
                                Logger.LogWarning(null, $"Строка пропущена из-за недостаточной длины. Позиция: {rowDataAreaStart}, длина: {actualRowLength}, требуемая длина: {rowParser.RowLength.ToString()}");

                                Interlocked.Increment(ref skippedRowsCount);
                            }
                            else
                            {
                                var rowLength = (int)Math.Min(rowBuffer.Length, actualRowLength);
                                if (actualRowLength > rowParser.RowLength.From)
                                {
                                    Logger.LogWarning(null, $"Строка строка имеет слишком большую длину. Позиция: {rowDataAreaStart}, длина: {actualRowLength}, требуемая длина: {rowParser.RowLength.ToString()}");
                                }

                                sourceFile.Position = rowDataAreaStart;
                                sourceFile.Read(rowBuffer, 0, rowLength);
                                var row = rowParser.ParseRow(rowBuffer);
                                foreach (var point in row.Points)
                                {
                                    resultFile.Write(point);
                                }

                                Interlocked.Increment(ref parsedRowsCount);
                            }
                        }
                        resultFile.Flush();
                     
                        return resultFile.BaseStream;
                    }
                }
            }

            FileStream getTempFileStream()
            {
                return new FileStream(Storaging.GetTempFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            }

            async Task<double> getDumpLengthInMegabytesAsync(OpenStreamAsyncDelegate rawDataStreamAsyncFactory)
            {
                using (var stream = await rawDataStreamAsyncFactory(new StreamParameters(1000), operationInfo)) // any
                {
                    return stream.Length / (1024D * 1024);
                }
            }

            // Return indexes on the first byte of the marker. Made it as robust as posible
            IEnumerable<long> findAllRowIndexes(Stream section, long sectionOffset)
            {
                using (section)
                {
                    var buffer = new byte[MAX_ROW_LENGTH + ROW_START_MARKER.Length];
                    while (true)
                    {
                        operationInfo.CancellationToken.ThrowIfCancellationRequested();

                        var readCount = section.Read(buffer, 0, buffer.Length);
                        var endOfStreamWasReached = readCount < buffer.Length;
                        if (readCount == 0)
                        {
                            yield break;
                        }
                        else if (endOfStreamWasReached)
                        {
                            // Populate the rest of the buffer with data that wont cause false detection
                            buffer.Set((byte)~ROW_START_MARKER.FirstElement(), readCount, buffer.Length - 1);
                        }

                        var bufferStartPosition = section.Position - readCount;
                        var bufferEndPosition = section.Position;
                        var lastFoundMarkerIndex = -1L; // Relative to the section start
                        for (int i = 0; i < readCount - ROW_START_MARKER.Length; i++)
                        {
                            var found = true;
                            for (int k = 0; k < ROW_START_MARKER.Length; k++)
                            {
                                if (buffer[i + k] != ROW_START_MARKER[k])
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
                                lastFoundMarkerIndex = bufferStartPosition + i;
                                yield return bufferStartPosition + i + sectionOffset;
                            }
                        }

                        if (lastFoundMarkerIndex == -1)
                        {
                            Logger.LogWarning(null, $"Ничего не найдено в диапазоне индексов: {bufferStartPosition} : {bufferEndPosition}");
                        }

                        if (endOfStreamWasReached)
                        {
                            Logger.LogInfo(null, $"Парсинг секции завершен");

                            break;
                        }
                        else
                        {
                            // Isn't setting position slow?
                            section.Position = lastFoundMarkerIndex == bufferEndPosition - ROW_START_MARKER.Length // if we found marker exactly at the end of buffer
                                ? bufferEndPosition // continue reading where we stopped
                                : bufferEndPosition - ROW_START_MARKER.Length; // take a bit of old buffer so that not to lost marker
                        }
                    }
                }
            }
        }
    }
}