using DeviceBase.IOModels.Protocols;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
#warning think over
    class MiddlewaredConnectionInterfaceDecorator : RUSConnectionInterfaceProxyBase, IInterfaceMiddlewareConveyor
    {
        readonly Dictionary<InterfaceMiddlewareOrder, List<IInterfaceMiddleware>> _middlewares = new Dictionary<InterfaceMiddlewareOrder, List<IInterfaceMiddleware>>()
        {
            { InterfaceMiddlewareOrder.ADAPTER, new List<IInterfaceMiddleware>() },
            { InterfaceMiddlewareOrder.WRAPPER, new List<IInterfaceMiddleware>() },
        };
        readonly IEnumerable<IInterfaceMiddleware> _conveyor;

        IEnumerable<Protocol> _supportedProtocols;
        public override IEnumerable<Protocol> SupportedProtocols => _supportedProtocols;

        public MiddlewaredConnectionInterfaceDecorator(IRUSConnectionInterface @base) : base(@base)
        {
            _conveyor = new Enumerable<IInterfaceMiddleware>()
            {
                _middlewares[InterfaceMiddlewareOrder.WRAPPER],
                _middlewares[InterfaceMiddlewareOrder.ADAPTER],
            };
            _supportedProtocols = base.SupportedProtocols;
        }

        public MiddlewaredConnectionInterfaceDecorator Register(IInterfaceMiddleware middleware, InterfaceMiddlewareOrder type)
        {
            _middlewares[type].Add(middleware);
#warning temp solution
            _supportedProtocols = _conveyor
                .Select(m => m
                    .Protocols
                    .Select(p => p.Value1))
                    .Flatten()
                    .Concat(base.SupportedProtocols);

            return this;
        }
        void IInterfaceMiddlewareConveyor.Register(IInterfaceMiddleware middleware, InterfaceMiddlewareOrder type)
        {
            Register(middleware, type);
        }

        public async override Task<IResponse> RequestAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
#warning add more smart paths
            foreach (var middleware in _conveyor)
            {
                request = await middleware.HandleOrMutateAsync(request, scope, operationInfo);
            }

            var response = await base.RequestAsync(request, scope, operationInfo);

            foreach (var middleware in _conveyor.Reverse())
            {
                response = await middleware.HandleOrMutateAsync(response, scope, operationInfo);
            }

            return response;
        }
    }
}
