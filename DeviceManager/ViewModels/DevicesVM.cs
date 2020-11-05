using System.Linq;
using Utilities.Extensions;
using MVVMUtilities.Types;
using DeviceBase;
using System.Collections.ObjectModel;
using Utilities;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using Common;
using Utilities.Types;
using System.ComponentModel;
using System.Threading.Tasks;
using DeviceBase.Models;
using TinyConfig;
using System.Threading;
using WidgetsCompositionRoot;
using RUSManagingTool.Models;
using System.IO;
using System;
using FilesExports;
using RUSManagingToolExports;

namespace RUSManagingTool.ViewModels
{
    public class DevicesVM : INotifyPropertyChanged, IScopeProvider
    {
        static readonly string TEMPLATES_DIR_PATH = Path.Combine(Environment.CurrentDirectory, "Files");

        public event PropertyChangedEventHandler PropertyChanged;
        public event ScopeChangedDelegate ScopeChanged;

        public BusyObject IsBusy { get; }
        public ObservableCollection<DeviceSlimVM> SupportedDevices { get; }
        public DeviceSlimVM SelectedDevice { get; set; }

        public IRUSDevice Scope => SelectedDevice?.RUSDevice;

        public DevicesVM(SerialPortVM serialPortVM, IRUSConnectionInterface connectionInterface, BusyObject isBusy)
        {
            IsBusy = isBusy;
            new DefaultFilesKeeper().CreateAllFiles(TEMPLATES_DIR_PATH, WidgetsLocator.Resolve<IFileExtensionFactory>());

            SupportedDevices = DevicesFactory
                .InstantiateSupported(connectionInterface)
                .OrderBy(d => d.Id)
                .Select(d => new DeviceSlimVM(serialPortVM, d, IsBusy, d.Children.OrderBy(cd => cd.Id).Select(cd => new DeviceSlimVM(serialPortVM, cd, IsBusy)).ToArray()))
                .ToObservable();
            PropertyChanged += _propertyHolder_PropertyChanged;
        }

        async void _propertyHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedDevice))
            {
                await selectDeviceAsync();
            }
        }

        DeviceSlimVM _previousDevice = null;
        async Task selectDeviceAsync()
        {
            using (IsBusy.BusyMode)
            {
                Logger.LogInfoEverywhere($"Выбрано устройство: \"{SelectedDevice.ToString()}\"");

                ScopeChanged?.Invoke(_previousDevice?.RUSDevice, SelectedDevice.RUSDevice);
                _previousDevice = SelectedDevice;
            }
        }
    }
}
