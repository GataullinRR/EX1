using DeviceBase.Devices;
using DeviceBase.IOModels;
using RUSManagingToolExports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;

namespace DataRequestWidget
{
    public class DataRequestVM : IDeviceHandler
    {
        readonly IRUSDevice _device;

        public DeviceDataRequestVM RequestVM { get; }
        public DeviceDataAutorequestVM AutorequestVM { get; }

        public DataRequestVM(IRUSDevice device, DeviceDataRequestVM requestVM, DeviceDataAutorequestVM autorequestVM)
        {
            _device = device;
            RequestVM = requestVM ?? throw new ArgumentNullException(nameof(requestVM));
            AutorequestVM = autorequestVM ?? throw new ArgumentNullException(nameof(autorequestVM));
        }

        public async Task OnConnectedAsync()
        {
            await AutorequestVM.StopAsync(CancellationToken.None);
            await _device.ActivateDeviceAsync(DeviceOperationScope.DEFAULT, new AsyncOperationInfo());
        }

        public async Task OnDisconnectedAsync()
        {
            await _device.DeactivateDeviceAsync(DeviceOperationScope.DEFAULT, new AsyncOperationInfo());
        }
    }
}
