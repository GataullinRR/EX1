using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    interface IInterfaceMiddleware
    {
        IEnumerable<Pair<Protocol, Protocol>> Protocols { get; }

        Task<IRequest> HandleOrMutateAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo);
        Task<IResponse> HandleOrMutateAsync(IResponse response, DeviceOperationScope scope, AsyncOperationInfo operationInfo);
    }

    abstract class MiddlewareBase : IInterfaceMiddleware
    {
        protected IRequest InitialRequest { get; private set; }

        public abstract IEnumerable<Pair<Protocol, Protocol>> Protocols { get; }

        public Task<IRequest> HandleOrMutateAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            InitialRequest = request;

            return handleOrMutateAsync(request, scope, operationInfo);
        }
        public virtual async Task<IRequest> handleOrMutateAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            return request;
        }

        public Task<IResponse> HandleOrMutateAsync(IResponse response, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            return handleOrMutateAsync(response, scope, operationInfo);
        }
        public virtual async Task<IResponse> handleOrMutateAsync(IResponse response, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            return response;
        }
    }

    enum InterfaceMiddlewareOrder
    {
        ADAPTER,
        WRAPPER
    }

    interface IInterfaceMiddlewareConveyor
    {
        void Register(IInterfaceMiddleware middleware, InterfaceMiddlewareOrder type);
    }
}
