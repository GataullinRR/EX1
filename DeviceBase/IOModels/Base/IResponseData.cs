using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Extensions;
using System.Linq;
using Utilities.Types;
using System.Collections.ObjectModel;

namespace DeviceBase.IOModels
{
    static class ResponseData 
    {
        public static readonly IResponseData NONE = new InMemoryResponseData(new byte[0]);
    }

    /// <summary>
    /// Do not use <see cref="IEnumerable{byte}"/> for big responses!
    /// </summary>
    public interface IResponseData : IEnumerable<byte>
    {
        long Count { get; }
        Task<byte[]> GetRangeAsync(long from, int count, AsyncOperationInfo operationInfo);
        Task<ReadOnlyCollection<byte>> GetAllAsync(AsyncOperationInfo operationInfo);
    }
}
