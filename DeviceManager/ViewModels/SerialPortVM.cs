using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.IO.Ports;
using WPFControls;
using MVVMUtilities.Types;
using TinyConfig;
using RUSManagingTool.Models;
using Common;
using WPFUtilities.Types;
using Ninject;
using IOBase;
using WidgetsCompositionRoot;

namespace RUSManagingTool.ViewModels
{
    public class SerialPortVM : NotifiableObjectTemplate
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("SerialPortVM");
        readonly static ConfigProxy<int> BAUD = CONFIG.Read(230400);

        readonly IInterface _port;
        readonly SerialPortOpenStateWatcher _portOpenWatcher;

        internal event EventHandler ConnectionEstablished;
        internal event EventHandler ConnectionClosed;

        public EnhancedObservableCollection<BoolOption> AvailablePorts { get; }
            = new EnhancedObservableCollection<BoolOption>();

        public ActionCommand Connect { get; }
        public ActionCommand Disconnect { get; }
        public bool Connected
        {
            get => _propertyHolder.Get(() => false);
            private set => _propertyHolder.Set(value);
        }
        public BusyObject IsBusy { get; }

        public SerialPortVM(IInterface port, BusyObject isBusy)
        {
            _port = port;
            IsBusy = isBusy;
            Connect = new ActionCommand(() => connectAsync());
            Disconnect = new ActionCommand(() => disconnectAsync()) { CanBeExecuted = false };

            _portOpenWatcher = new SerialPortOpenStateWatcher(port);
            _portOpenWatcher.Closed += COMPortVM_Closed;

            updateAvailablePortsAsync();

            /////////////////////////////////////////////

            void COMPortVM_Closed(object sender, EventArgs e)
            {
                disconnectAsync();
            }

            async void connectAsync()
            {
                using (await lockPortAndUI())
                {
                    var portToConnect = AvailablePorts.FindOrDefault(p => p.IsChecked);
                    var rawPortName = (string)portToConnect?.Tag;
                    var escapedPortName = portToConnect?.OptionName;
                    try
                    {
                        _port.Open(rawPortName, BAUD);
                        if (_port.IsOpen)
                        {
                            Logger.LogOKEverywhere($"Порт {escapedPortName} открыт");

                            Connect.CanBeExecuted = false;
                            Disconnect.CanBeExecuted = true;
                            Connected = true;
                            ConnectionEstablished?.Invoke(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Не удалось открыть порт {escapedPortName}", $"-MSG ({rawPortName})", ex);
                    }
                }
            }

            // This method can be called twice
            async void disconnectAsync()
            {
                using (await lockPortAndUI())
                {
                    try
                    {
                        var wasOpened = _port.IsOpen;
                        if (_port.IsOpen)
                        {
                            _port.Close();
                        }
                        if (Connected)
                        {
                            Connect.CanBeExecuted = true;
                            Disconnect.CanBeExecuted = false;
                            Connected = false;
                            ConnectionClosed?.Invoke(this);

                            Logger.LogOKEverywhere($"Порт закрыт");
                        }
                    }
                    catch
                    {

                    }
                }
            }

            async Task<IDisposable> lockPortAndUI()
            {
                var busyHolder = IsBusy.BusyMode;
                var syncHolder = await _port.Locker.AcquireAsync();

                return new DisposingActions { syncHolder, busyHolder };
            }

            async void updateAvailablePortsAsync()
            {
                while (true)
                {
                    var ports = _port.PortNames
                        .OrderBy()
                        .Select(p => new BoolOption()
                        {
                            Tag = p,
                            // char.IsDigit - because sometimes there can be awkward names with chinese symbols
                            OptionName = p.StartsWith("COM")
                                ? p.Remove("COM").TakeWhile(char.IsDigit).Aggregate()
                                : p
                        })
                        .ToArray();
                    var previousleSelectedPort = AvailablePorts.Find(p => p.IsChecked).ValueOrDefault?.OptionName;
                    using (AvailablePorts.EventSuppressingMode)
                    {
                        AvailablePorts.Clear();
                        AvailablePorts.AddRange(ports);
                    }

                    Connect.CanBeExecuted = true;
                    var previouslySelectedOption = AvailablePorts.Find(p => p.OptionName == previousleSelectedPort);
                    if (previouslySelectedOption.Found)
                    {
                        previouslySelectedOption.Value.IsChecked = true;
                    }
                    else if (AvailablePorts.Count != 0)
                    {
                        AvailablePorts.FirstElement().IsChecked = true;
                    }
                    else
                    {
                        // There is no port selected
                        Connect.CanBeExecuted = false;
                    }

                    await Task.Delay(1000);
                    // To stop ports' list update
                    await CommonUtils.LoopWhileTrueAsync(() => _portOpenWatcher.IsOpen, 1000);
                }
            }
        }
    }
}
