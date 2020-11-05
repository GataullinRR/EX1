using DataViewExports;
using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;

namespace FlashDumpLoaderWidget
{
    class FlashDumpRowsReader : IRowsReader
    {
        static readonly int READER_DEGREE_OF_PARALLELISM = Environment.ProcessorCount;
        static readonly int RANDOM_READER_DEGREE_OF_PARALLELISM = Environment.ProcessorCount;

        readonly ObjectPool<BinaryReader> _streams;
        readonly int _numOfPointsInsideRow;
        public int RowsCount { get; }

        public FlashDumpRowsReader(int numOfPointsInsideRow, int rowsCount, Func<Task<Stream>> mainStreamfactory)
        {
            _numOfPointsInsideRow = numOfPointsInsideRow;
            _streams = new ObjectPool<BinaryReader>(() => mainStreamfactory().ThenDo(s => new BinaryReader(s)));
            RowsCount = rowsCount;
        }

        public async Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, int rowsCount, AsyncOperationInfo operationInfo)
        {
            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            var indexes = rowsCount
                .Range()
                .Select(i => i + firstRowIndex)
                .ToArray();
            return await readRowsAsync(firstRowIndex, indexes, true, operationInfo);
        }

        public Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, IList<int> rowsIndexes, AsyncOperationInfo operationInfo)
        {
            return readRowsAsync(firstRowIndex, rowsIndexes, false, operationInfo);
        }

        async Task<IPointsRow[]> readRowsAsync(int firstRowIndex, IList<int> rowsIndexes, bool isIndexesSequential, AsyncOperationInfo operationInfo)
        {
            await ThreadingUtils.ContinueAtThreadPull(operationInfo);

            int DEGREE_OF_PARALLELISM = isIndexesSequential 
                ? READER_DEGREE_OF_PARALLELISM 
                : RANDOM_READER_DEGREE_OF_PARALLELISM;

            var futures = new Task[DEGREE_OF_PARALLELISM];
            var workGroups = rowsIndexes.SplitOnGroups(DEGREE_OF_PARALLELISM);
            var offset = 0;
            var rows = new IPointsRow[rowsIndexes.Count];
            for (int i = 0; i < DEGREE_OF_PARALLELISM; i++)
            {
                var work = workGroups[i];
                futures[i] = readRowsTo(offset, work);
                offset += work.Length;
            }
            Task.WaitAll(futures);

            return rows;

            async Task readRowsTo(int rowsArrayOffset, IList<int> indexes)
            {
                await ThreadingUtils.ContinueAtThreadPull(operationInfo);

                var reader = await _streams.AquireAsync(operationInfo);
                try
                {
                    var rowBuffer = new byte[_numOfPointsInsideRow * sizeof(double)];
                    if (isIndexesSequential && indexes.Count > 0)
                    {
                        reader.BaseStream.Position = indexes[0] * _numOfPointsInsideRow * sizeof(double);
                    }
                    for (int i = 0; i < indexes.Count; i++)
                    {
                        if (!isIndexesSequential)
                        {
                            var index = indexes[i];
                            reader.BaseStream.Position = index * _numOfPointsInsideRow * sizeof(double);
                        }

                        var row = new double[_numOfPointsInsideRow];
                        reader.BaseStream.Read(rowBuffer, 0, rowBuffer.Length); // Hope the amount of data will be enough)
                        for (int k = 0; k < _numOfPointsInsideRow; k++)
                        {
                            row[k] = readDoubleFast(rowBuffer, k * sizeof(double));
                            //row[k] = reader.ReadDouble(); // Bottleneck (15%)
                        }

                        rows[rowsArrayOffset + i] = new PointsRow(row);
                    }
                }
                finally
                {
                    await _streams.ReleaseAsync(reader, operationInfo);
                }
            }
        }


        /// <summary>
        /// <see cref="BinaryReader.ReadDouble"/> is too slow because of too many internal checks
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        unsafe double readDoubleFast(byte[] buffer, int offset)
        {
            uint lo = (uint)(buffer[offset + 0] | buffer[offset + 1] << 8 |
                buffer[offset + 2] << 16 | buffer[offset + 3] << 24);
            uint hi = (uint)(buffer[offset + 4] | buffer[offset + 5] << 8 |
                buffer[offset + 6] << 16 | buffer[offset + 7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }
    }
}
