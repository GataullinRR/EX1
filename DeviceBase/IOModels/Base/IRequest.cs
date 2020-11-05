using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public interface IRequest
    {
        RequestTimeout Timeout { get; }
        IEnumerable<byte> Serialized { get; }

        /// <summary>
        /// Can throw an exception
        /// </summary>
        /// <param name="inputStream">Must be thread safe</param>
        /// <param name="operationInfo">Must be thread safe</param>
        /// <returns></returns>
        Task<IResponse> DeserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo);
        IResponse BuildErrorResponse(RequestStatus status);
    }

    public interface IRequest<TResponse> : IRequest
        where TResponse : IResponse
    {
        /// <summary>
        /// Can throw an exception
        /// </summary>
        /// <param name="inputStream">Must be thread safe</param>
        /// <param name="operationInfo">Must be thread safe</param>
        /// <returns></returns>
        new Task<TResponse> DeserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo);
        new TResponse BuildErrorResponse(RequestStatus status);
    }
}
