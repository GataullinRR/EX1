using DeviceBase.Devices;
using System;
using static VirtualDevice.VirtualRUSDeviceBase;

namespace VirtualDevice
{
    class RequestHandlerInfo
    {
        public delegate byte[] HandleDelegate(byte[] requestBody, bool isReadRequest);

        public Command Address { get; }
        public HandleDelegate Handler { get; }

        public RequestHandlerInfo(Command address, HandleDelegate handler)
        {
            Address = address;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}
