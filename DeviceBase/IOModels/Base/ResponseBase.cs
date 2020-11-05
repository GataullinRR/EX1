using System;
using System.Collections.Generic;
using DeviceBase.Devices;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    abstract class ResponseBase<TRequest> : IResponse
        where TRequest : class, IRequest
    {
        public TRequest Request { get; }
        public RequestStatus Status { get; }
        public IResponseData Data { get; }
        /// <summary>
        /// It's the same thing as <see cref="Data"/> but without Header (the command answered) and Checksum. 
        /// For <see cref="Command.NONE"/> is equal to <see cref="Data"/>
        /// </summary>
        public IDictionary<Key, IResponseData> DataSections { get; }

        IRequest IResponse.Request => Request;

        public ResponseBase(TRequest request, RequestStatus status)
            : this(request, status, null)
        {

        }
        public ResponseBase(TRequest request, RequestStatus status, IResponseData data)
            : this(request, status, data, new Dictionary<Key, IResponseData>())
        {

        }
        public ResponseBase(TRequest request, RequestStatus status, IResponseData data, IDictionary<Key, IResponseData> dataSections)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Status = status;
            Data = data;
            DataSections = dataSections ?? throw new ArgumentNullException(nameof(dataSections));
        }
    }
}
