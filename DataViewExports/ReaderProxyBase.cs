using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace DataViewExports
{
    public abstract class ReaderProxyBase : IRowsReader
    {
        readonly IRowsReader _base;

        public int RowsCount => _base.RowsCount;

        protected ReaderProxyBase(IRowsReader @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, int rowsCount, AsyncOperationInfo operationInfo)
        {
            return _base.ReadRowsAsync(firstRowIndex, rowsCount, operationInfo);
        }

        public Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, IList<int> rowsIndexes, AsyncOperationInfo operationInfo)
        {
            return _base.ReadRowsAsync(firstRowIndex, rowsIndexes, operationInfo);
        }
    }
}
