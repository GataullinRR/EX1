using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using VirtualDevice;
using Utilities.Extensions;
using Ninject;
using IOBase;
using DeviceBase.IOModels;

namespace RUSManagingTool.Models
{
    class TestCOMInterface : IInterface
    {
        public string PortName => "TCOM";
        readonly SalachovDeviceSet _stream = new SalachovDeviceSet();

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public IPipe Pipe => _stream;
        public SemaphoreSlim Locker { get; }
        public bool IsOpen { get; private set; }
        public IEnumerable<string> PortNames => new[] { PortName };
        public InterfaceDevice CurrentInterfaceDevice => InterfaceDevice.COM;

        public TestCOMInterface(SemaphoreSlim locker)
        {
            Locker = locker ?? throw new ArgumentNullException(nameof(locker));
        }

        public byte[] ClearReadBuffer()
        {
            return _stream.PopInputBuffer();
        }

        public void Close()
        {
            IsOpen = false;
            ConnectionClosed?.Invoke(this);
        }

        public void Open(string portName, int baudRate)
        {
            IsOpen = true;
            ConnectionEstablished?.Invoke(this);
        }
    }
}
