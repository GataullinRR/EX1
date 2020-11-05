using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBase.Devices
{
    public class StatusFeature : IRUSDeviceFeature
    {
        public event Action<DeviceStatusInfo> StatusAcquired;

        internal StatusFeature() { }

        internal void FireStatusAcquired(DeviceStatusInfo statusInfo)
        {
            StatusAcquired?.Invoke(statusInfo);
        }
    }
}
