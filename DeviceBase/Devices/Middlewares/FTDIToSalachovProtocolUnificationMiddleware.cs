using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    class FTDIToSalachovProtocolUnificationMiddleware : MiddlewareBase
    {
        readonly IRUSConnectionInterface _connectionInterface;

        public override IEnumerable<Pair<Protocol, Protocol>> Protocols => new[]
        {
            new Pair<Protocol, Protocol>(Protocol.SALAHOV, Protocol.FTDI_BOX),
            new Pair<Protocol, Protocol>(Protocol.FTDI_BOX, Protocol.FTDI_BOX),
        };

        public FTDIToSalachovProtocolUnificationMiddleware(IRUSConnectionInterface connectionInterface)
        {
            _connectionInterface = connectionInterface ?? throw new ArgumentNullException(nameof(connectionInterface));
        }

        public override async Task<IRequest> handleOrMutateAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            var salachovRequest = request.As<SalachovRequest>();
            var ftdiBoxRequest = request.As<FTDIBoxRequest>();

            if (_connectionInterface.InterfaceDevice == InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX && salachovRequest != null)
            {
                return FTDIBoxRequest.CreateDeviceRequest(request, scope);
            }
            else if (_connectionInterface.InterfaceDevice == InterfaceDevice.COM && ftdiBoxRequest != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return request;
            }
        }

        public override async Task<IResponse> handleOrMutateAsync(IResponse response, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            if (_connectionInterface.InterfaceDevice == InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX
                && InitialRequest is SalachovRequest
                && response is FTDIBoxResponse ftdiBoxResponse
                && ftdiBoxResponse.Status == RequestStatus.OK)
            {
                return await InitialRequest.DeserializeResponseAsync(new ResponseDataToResponseFutureAdapter(ftdiBoxResponse.Body), operationInfo); // Will return SalachovResponse
            }
            else
            {
                return response;
            }
        }
    }
}
