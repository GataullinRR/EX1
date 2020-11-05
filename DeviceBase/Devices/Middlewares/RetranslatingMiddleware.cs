using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class RetranslatingMiddleware : MiddlewareBase
    {
        readonly RUSDeviceId _parrentId;

        bool _handled = true;

        public override IEnumerable<Pair<Protocol, Protocol>> Protocols { get; } = new[]
        {
            new Pair<Protocol, Protocol>(Protocol.SALAHOV, Protocol.SALAHOV)
        };

        public RetranslatingMiddleware(RUSDeviceId parrentId)
        {
            _parrentId = parrentId;
        }

        public override async Task<IRequest> handleOrMutateAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            if (request is SalachovRequest salachovRequest 
                && salachovRequest.DeviceId != _parrentId 
                && !salachovRequest.IsBroadcast)
            {
                _handled = true;

                return SalachovRequest.CreateWriteRequest(_parrentId,
                       Devices.Command.RETRANSLATE_PACKET,
                       Requests.GetRequestDescription(_parrentId, Devices.Command.RETRANSLATE_PACKET)
                           .Descriptors
                           .Single()
                           .InstantiateEntity(request.Serialized).ToSequence(),
                       scope);
            }
            else
            {
                _handled = false;

                return request;
            }
        }

        public override async Task<IResponse> handleOrMutateAsync(IResponse response, DeviceOperationScope scope, AsyncOperationInfo operationInfo)
        {
            if (response.Status == RequestStatus.OK && _handled)
            {
                return await InitialRequest.DeserializeResponseAsync(
                    new ResponseDataToResponseFutureAdapter(response.To<SalachovResponse>().Body),
                    operationInfo);
            }
            else
            {
                return response;
            }
        }
    }
}
