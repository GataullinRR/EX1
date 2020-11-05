using System;

namespace DeviceBase.IOModels
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    class RequestStatusMappingAttribute : Attribute
    {
        public RequestStatusMappingAttribute(params RequestStatus[] requestStatuses)
        {
            RequestStatuses = requestStatuses ?? throw new ArgumentNullException(nameof(requestStatuses));
        }

        public RequestStatus[] RequestStatuses { get; }
    }
}
