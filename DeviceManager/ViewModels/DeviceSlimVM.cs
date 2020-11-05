using System.Linq;
using Utilities.Extensions;
using MVVMUtilities.Types;
using DeviceBase.Devices;
using System;
using WidgetsCompositionRoot;

namespace RUSManagingTool.ViewModels
{
    public class DeviceSlimVM
    {
        readonly BusyObject _busy;
        
        public WidgetsVM Widgets { get; }
        public DeviceSlimVM[] Children { get; }
        public IRUSDevice RUSDevice { get; }

        public DeviceSlimVM(SerialPortVM serialPortVM, IRUSDevice device, BusyObject busy)
            : this(serialPortVM, device, busy, null)
        {

        }

        public DeviceSlimVM(SerialPortVM serialPortVM, IRUSDevice device, BusyObject busy, DeviceSlimVM[] children)
        {
            RUSDevice = device ?? throw new ArgumentNullException(nameof(device));
            _busy = busy ?? throw new ArgumentNullException(nameof(busy));
            Widgets = new WidgetsVM(WidgetsLocator.ResolveWidgetsForScope(RUSDevice, _busy).ToArray());
            Children = this.ToSequence().Concat(children.NullToEmpty()).ToArray();
        }

        public override string ToString()
        {
            return $"ID:{(byte)RUSDevice.Id:D2}-{RUSDevice.Name}";
        }
    }
}
