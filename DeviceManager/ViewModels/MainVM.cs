using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.IO.Ports;
using System.Threading;
using RUSManagingTool.Models;
using MVVMUtilities.Types;
using Ninject;
using Ninject.Parameters;
using DeviceBase.IOModels;
using System.ComponentModel;
using FTD2XXSerialPort;
using TinyConfig;
using Common;
using RUSManagingToolExports;

namespace RUSManagingTool.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        static readonly ConfigAccessor CONFIG = Configurable.CreateConfig("Timeouts");
        static readonly ConfigProxy<int> FTDI_TIMEOUT = CONFIG.Read(3000);
        static readonly ConfigProxy<int> COM_TIMEOUT = CONFIG.Read(3000);

        public event PropertyChangedEventHandler PropertyChanged;

        public SerialPortVM COMPortVM { get; private set; }
        public DevicesVM DevicesVM { get; private set; }
        public LogVM LogVM { get; private set; }

        public MainVM()
        {
            var locker = new SemaphoreSlim(1, 1);
            var port = new InterfaceAggregator(
                new COMPortAdapter(new SerialPort(), COM_TIMEOUT, COM_TIMEOUT, locker),
                new TestCOMInterface(locker),
                new FTDIInterface(FTDI_TIMEOUT, FTDI_TIMEOUT, locker),
                new TestFTDIInterface(locker));

            var busy = new BusyObject();
            COMPortVM = new SerialPortVM(port, busy);
            DevicesVM = new DevicesVM(COMPortVM, new SerialPortRUSConnectionInterface(port), busy);
            LogVM = new LogVM();

            new WidgetsManager(COMPortVM, DevicesVM, busy);
        }
    }
}
