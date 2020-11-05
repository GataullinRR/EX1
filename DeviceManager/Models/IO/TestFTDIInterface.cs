using System;
using System.Collections.Generic;
using System.Threading;
using VirtualDevice;
using Utilities.Extensions;
using IOBase;
using DeviceBase.IOModels;

namespace RUSManagingTool.Models
{
    class TestFTDIInterface : IInterface
    {
        public string PortName => "TFTDI";
        readonly FTDIBoxDeviceSet _stream = new FTDIBoxDeviceSet();

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public IPipe Pipe => _stream;
        public SemaphoreSlim Locker { get; }
        public bool IsOpen { get; private set; }
        public IEnumerable<string> PortNames => new[] { PortName };
        public InterfaceDevice CurrentInterfaceDevice => InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX;

        public TestFTDIInterface(SemaphoreSlim locker)
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
