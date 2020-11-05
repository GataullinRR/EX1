using System;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Utilities.Extensions;
using IOBase;
using DeviceBase.IOModels;

namespace RUSManagingTool.Models
{
    public class COMPortAdapter : IInterface
    {
        readonly SerialPort _port;

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public SemaphoreSlim Locker { get; }
        public string PortName => _port.PortName;
        public IPipe Pipe { get; set; }
        public bool IsOpen => _port.IsOpen;
        public IEnumerable<string> PortNames => SerialPort.GetPortNames();
        public InterfaceDevice CurrentInterfaceDevice => InterfaceDevice.COM;

        public COMPortAdapter(SerialPort port, int readTimeout, int writeTimeout, SemaphoreSlim locker)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            Locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _port.ReadTimeout = readTimeout;
            _port.WriteTimeout = writeTimeout;
            Pipe = new COMPortPipe(_port);
        }

        public void Open(string portName, int baudRate)
        {
            _port.PortName = portName;
            _port.BaudRate = baudRate;
            _port.Open();
            if (_port.IsOpen)
            {
                ConnectionEstablished?.Invoke(this);
            }
        }

        public void Close()
        {
            _port.Close();
            if (!_port.IsOpen)
            {
                ConnectionClosed?.Invoke(this);
            }
        }
    }
}