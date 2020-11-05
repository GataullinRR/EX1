using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using DeviceBase.IOModels;

namespace IOBase
{
    public interface IInterface
    {
        string PortName { get; }
        IPipe Pipe { get; }
        /// <summary>
        /// Is used to synchronize acces to the port
        /// </summary>
        SemaphoreSlim Locker { get; }
        bool IsOpen { get; }
        IEnumerable<string> PortNames { get; }
        InterfaceDevice CurrentInterfaceDevice { get; }

        event EventHandler ConnectionEstablished;
        event EventHandler ConnectionClosed;

        void Open(string portName, int baudRate);
        void Close();
    }
}