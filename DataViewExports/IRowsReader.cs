using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DataViewExports
{
    /// <summary>
    /// Must be thread safe
    /// </summary>
    public interface IRowsReader
    {
        int RowsCount { get; }
        Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, int rowsCount, AsyncOperationInfo operationInfo);
        Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, IList<int> rowsIndexes, AsyncOperationInfo operationInfo);
    }
}
