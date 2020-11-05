using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;
using System.Threading;
using RUSManagingTool.Models;
using System.IO;
using IOBase;
using DeviceBase.IOModels;

namespace RUSManagingTool.Models
{
    public class InterfaceAggregator : IInterface
    {
        readonly IInterface[] _interfaces;
        IInterface _selectedInterface;

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public IEnumerable<string> PortNames => _interfaces.Select(p => p.PortNames).Flatten();
        public string PortName => _selectedInterface.PortName;
        public IPipe Pipe => _selectedInterface.Pipe;
        public SemaphoreSlim Locker => _selectedInterface.Locker;
        public bool IsOpen => _selectedInterface.IsOpen;
        public InterfaceDevice CurrentInterfaceDevice => _selectedInterface.CurrentInterfaceDevice;

        public InterfaceAggregator(params IInterface[] interfaces)
        {
            _interfaces = interfaces?.ToArray() ?? throw new ArgumentNullException(nameof(interfaces));
            _selectedInterface = interfaces.First();
        }

        public void Open(string portName, int baudRate)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException();
            }
            else
            {
                _selectedInterface = _interfaces.SingleOrDefault(p => p.PortNames.Contains(portName)) ?? _selectedInterface;
                _selectedInterface.Open(portName, baudRate);
                ConnectionEstablished?.Invoke(this);
            }
        }

        public void Close()
        {
            _selectedInterface.Close();
            ConnectionClosed?.Invoke(this);
        }
    }
}
