using DataViewExports;
using DeviceBase.Models;
using System.Threading.Tasks;
using Utilities.Types;

namespace DataViewWidget
{
    /// <summary>
    /// Is used for optimization. Decreases amount of points to be rendered
    /// </summary>
    interface IDecimatedRowsReader
    {
        IRowsReader Source { get; }
        /// <summary>
        /// Will return less elements than <paramref name="count"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <param name="operationInfo"></param>
        /// <returns></returns>
        Task<IPointsRow[]> GetDecimatedRangeAsync(int from, int count, AsyncOperationInfo operationInfo);
    }

    class dsd
    {
        async Task Loop()
        {
            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(true);

            }
        }
    }
}
