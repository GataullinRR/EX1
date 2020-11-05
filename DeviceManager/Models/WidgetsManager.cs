using Common;
using DeviceBase.Devices;
using MVVMUtilities.Types;
using RUSManagingTool.ViewModels;
using RUSManagingToolExports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WidgetsCompositionRoot;

namespace RUSManagingTool.Models
{
    class WidgetsManager
    {
        readonly IScopeProvider _scopeProvider;
        readonly BusyObject _busy;

        public WidgetsManager(SerialPortVM serialPortVM, IScopeProvider scopeProvider, BusyObject busy)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _busy = busy ?? throw new ArgumentNullException(nameof(busy));
            serialPortVM.ConnectionClosed += SerialPortVM_ConnectionClosed;
            serialPortVM.ConnectionEstablished += SerialPortVM_ConnectionEstablished;
            _scopeProvider.ScopeChanged += _scopeProvider_ScopeChanged;
        }

        async void SerialPortVM_ConnectionEstablished(object sender, EventArgs e)
        {
            await execute(_scopeProvider.Scope, dm => dm.OnConnectedAsync());
        }

        async void SerialPortVM_ConnectionClosed(object sender, EventArgs e)
        {
            await execute(_scopeProvider.Scope, dm => dm.OnDisconnectedAsync());
        }

        async void _scopeProvider_ScopeChanged(DeviceBase.Devices.IRUSDevice oldScope, DeviceBase.Devices.IRUSDevice newScope)
        {
            using (_busy.BusyMode)
            {
                await execute(oldScope, dm => dm.OnDisconnectedAsync());
                await execute(newScope, dm => dm.OnConnectedAsync());
            }
        }

        async Task execute(IRUSDevice scope, Func<IDeviceHandler, Task> executor)
        {
            using (_busy.BusyMode)
            {
                if (scope != null)
                {
                    foreach (var deviceModel in WidgetsLocator.GetDeviceHandler(scope))
                    {
                        try
                        {
                            await executor(deviceModel);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Ошибка отключения устройства", $"-MSG. Устройство: {scope}", ex);
                        }
                    }
                }
            }
        }
    }
}
