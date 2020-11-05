using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.Threading;
using System.IO.Ports;
using IOBase;

namespace RUSManagingTool.Models
{
    class SerialPortOpenStateWatcher
    {
        const int UPDATE_PERIOD = 100;
        readonly IInterface _port;

        public event EventHandler Closed;
        public bool IsOpen { get; private set; }

        public SerialPortOpenStateWatcher(IInterface port)
        {
            _port = port;
            watchLoop();

            async void watchLoop()
            {
                while (true)
                {
                    IsOpen = await CommonUtils.ExecuteSynchronouslyAsync(() => _port.IsOpen, _port.Locker);
                    if (IsOpen)
                    {
                        await CommonUtils.LoopWhileTrueAsync(() => _port.IsOpen, UPDATE_PERIOD, _port.Locker);
                        Closed?.Invoke(this);
                    }
                    else
                    {
                        await CommonUtils.LoopWhileTrueAsync(() => !_port.IsOpen, UPDATE_PERIOD, _port.Locker);
                    }
                }
            }
        }
    }
}
