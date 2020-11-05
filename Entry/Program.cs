using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using RUSManagingTool;
using System.Runtime.InteropServices;
using System.Windows;
using Ninject;
using System.Threading;
using MVVMUtilities.Types;
using RUSManagingTool.Models;
using System.IO.Ports;
using DeviceBase.IOModels;
using RUSManagingTool.ViewModels;

namespace IoCInitilizer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            setupAndRun();
        }

        static void setupAndRun()
        {
            var kernel = new StandardKernel();

            kernel.Bind<SemaphoreSlim>().ToMethod(c => new SemaphoreSlim(1));
            kernel.Bind<SemaphoreSlim>().To<SemaphoreSlim>().InSingletonScope().Named("PortLocker").WithConstructorArgument(1);
            kernel.Bind<BusyObject>().To<BusyObject>().InSingletonScope();
            kernel.Bind<ISerialPort>().To<SerialPortWrapper>().InSingletonScope().Named("RootSerial");
            kernel.Bind<ISerialPort>().To<TestSerialPort>().InSingletonScope().Named("RootSerial");
            kernel.Bind<SerialPort>().To<SerialPort>().InSingletonScope();
            kernel.Bind<ISerialPort>().To<SerialPortAggregator>().InSingletonScope().Named("MainPort").WithConstructorArgument(c => c.Kernel.GetAll<ISerialPort>("RootSerial").ToArray());
            kernel.Bind<IRUSConnectionInterface>().To<COMPortRUSConnectionInterface>().InSingletonScope().WithConstructorArgument(c => c.Kernel.Get<ISerialPort>("MainPort"));
            kernel.Bind<COMPortVM>().To<COMPortVM>().InSingletonScope().WithConstructorArgument(c => c.Kernel.Get<ISerialPort>("MainPort"));
            kernel.Bind<LogVM>().To<LogVM>().InSingletonScope();
            kernel.Bind<MainVM>().To<MainVM>().InSingletonScope();
            kernel.Bind<IMainWindow>().To<MainWindow>().InSingletonScope();

            var app = new App();
            app.InitializeComponent();
            app.SetMainWindowProvider(() => kernel.Get<IMainWindow>());
            app.Run();
        }
    }
}
