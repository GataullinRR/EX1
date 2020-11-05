using Common;
using Controls;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;
using WPFUtilities.Types;

namespace RUSModuleSetDirrectionWidget
{
    public class RUSModuleSetDirectionVM : INotifyPropertyChanged
    {
        readonly IRUSDevice _device;

        public event PropertyChangedEventHandler PropertyChanged;

        public BusyObject IsBusy { get; }
        public EnhancedObservableCollection<CommandEntityVM> Entities { get; }
        public ActionCommand<Command> SendCommand { get; }

        public RUSModuleSetDirectionVM(IRUSDevice device, BusyObject busy)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            IsBusy = busy ?? throw new ArgumentNullException(nameof(busy));

            var descriptors = Requests.GetRequestDescription(_device.Id, Command.KEEP_MTF); // All other commands have the same body
            Entities = new EnhancedObservableCollection<CommandEntityVM>(
                descriptors.UserCommandDescriptors.Select(d => new CommandEntityVM(d)));

            SendCommand = new ActionCommand<Command>(sendAsync, IsBusy);

            async Task sendAsync(CommandParameter<Command> command)
            {
                using (IsBusy.BusyMode)
                {
                    assertViewValues();

                    if (command.IsSet)
                    {
                        await _device.BurnAsync(command.Value, Entities.Select(e => e.Entity), null, null);

                        Logger.LogOKEverywhere($"Команда отправлена");
                    }
                    else
                    {
                        Debugger.Break(); // Binding error
                    }
                }
            }
        }

        void assertViewValues()
        {
            foreach (var e in Entities)
            {
                e.EntityValue.AssertValue();
            }
        }
    }
}
