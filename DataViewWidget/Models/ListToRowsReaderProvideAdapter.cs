using DataViewExports;
using DeviceBase.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;

namespace DataViewWidget
{
    class ListToRowsReaderProviderAdapter : IRowsReaderProvider
    {
        class SyncReader : IRowsReader
        {
            readonly IList<IPointsRow> _base;

            public int RowsCount => _base.Count;

            public SyncReader(IList<IPointsRow> @base)
            {
                _base = @base;
            }

            public Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, int rowsCount, AsyncOperationInfo operationInfo)
            {
                var result = _base.GetRange(firstRowIndex, rowsCount).ToArray();

                return Task.FromResult(result);
            }

            public Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, IList<int> rowsIndexes, AsyncOperationInfo operationInfo)
            {
                var result = new IPointsRow[rowsIndexes.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = _base[rowsIndexes[i]];
                }

                return Task.FromResult(result);
            }
        }

        public IRowsReader RowsReader { get; }

        public ListToRowsReaderProviderAdapter(IList<IPointsRow> @base)
        {
            RowsReader = new SyncReader(@base);
        }
    }
}
