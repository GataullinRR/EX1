using DeviceBase.Devices;

namespace DeviceBase.IOModels
{
    public interface ISalachovRequest : IRequest
    {
        RUSDeviceId DeviceId { get; }
        Command Address { get; }
    }
}
