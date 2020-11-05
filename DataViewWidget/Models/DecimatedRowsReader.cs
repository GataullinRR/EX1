using DataViewExports;
using DeviceBase.Models;
using System.Linq;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DataViewWidget
{
    class DecimatedRowsReader : IDecimatedRowsReader
    {
        public IRowsReader Source { get; }

        readonly int _maxRowsCount;

        public DecimatedRowsReader(IRowsReader source, int maxRowsCount)
        {
            Source = source;
            _maxRowsCount = maxRowsCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <param name="operationInfo"></param>
        /// <returns></returns>
        public async Task<IPointsRow[]> GetDecimatedRangeAsync(int from, int count, AsyncOperationInfo operationInfo)
        {
#warning what if underlying collection is not thread safe? Or it decreases its size?!
            await ThreadingUtils.ContinueAtThreadPull(operationInfo);
            
            IPointsRow[] result;
            if (count > _maxRowsCount) // Decimation required
            {
                var coefficient = (double)count / _maxRowsCount;
                var sourceRows = _maxRowsCount
                    .Range()
                    .AsParallel()
                    .AsOrdered()
                    .Select(i => (i * coefficient + from).Round())
                    .Where(i => i < Source.RowsCount) // To handle possible rounding error
                    .ToArray();
                return await Source.ReadRowsAsync(from, sourceRows, operationInfo);
            }
            else
            {
                result = await Source.ReadRowsAsync(from, count, operationInfo);
            }

            return result;
        }
    }
}
