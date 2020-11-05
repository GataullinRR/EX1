using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MVVMUtilities.Types;
using DeviceBase;
using System.Linq;
using Utilities.Extensions;
using DeviceBase.Devices;
using WPFUtilities.Types;
using System.Threading;
using DeviceBase.IOModels;
using Common;
using System.Windows;
using Utilities.Types;

namespace StatusWidget
{
    public class DeviceStatusVM
    {
        readonly IRUSDevice _device;
        readonly BusyObject _isBusy;
        readonly PeriodDelay _statusUpdateDelay = new PeriodDelay(1000);

        public ActionCommand Update { get; }

        public EnhancedObservableCollection<string> Flags { get; }
            = new EnhancedObservableCollection<string>();
        public EnhancedObservableCollection<KeyValuePair<string, string>> Info { get; }
            = new EnhancedObservableCollection<KeyValuePair<string, string>>();

        public DeviceStatusVM(IRUSDevice device, BusyObject isBusy)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _isBusy = isBusy ?? throw new ArgumentNullException(nameof(isBusy));

            _device.TryGetFeature<StatusFeature>().StatusAcquired += DeviceStatusVM_StatusAcquired;

            Update = new ActionCommand(update, () => !_isBusy, _isBusy);

            async Task update()
            {
                using (_isBusy.BusyMode)
                {
                    var statusReadResult = await _device.TryReadStatusAsync(DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (statusReadResult.Status != ReadStatus.OK)
                    {
                        Info.Clear();
                        Flags.Clear();
                    }
                    else
                    {
                        Logger.LogOKEverywhere("Статус обновлен");
                    }
                }
            }
        }

        async void DeviceStatusVM_StatusAcquired(DeviceStatusInfo StatusInfo)
        {
            if (_statusUpdateDelay.TimeLeft > 0)
            {
                return;
            }
            else
            {
                await _statusUpdateDelay.WaitTimeLeftAsync(); // Restart
            }

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                using (Flags.EventSuppressingMode)
                {
                    Flags.Clear();
                    Flags.AddRange(StatusInfo.CurrentStatuses);
                }
                using (Info.EventSuppressingMode)
                {
                    Info.Clear();
                    if (StatusInfo.SerialNumber.HasValue)
                    {
                        Info.Add(new KeyValuePair<string, string>("Серийный номер", StatusInfo.SerialNumber.Value.ToStringInvariant()));
                    }
                    var flags = Convert
                        .ToString(StatusInfo.Status.Bits, 2)
                        .PadLeft(StatusInfo.Status.NumOfBits, '0')
                        .Reverse()
                        .GroupBy(4)
                        .Select(g => g.Aggregate())
                        .Aggregate(" ")
                        .Reverse()
                        .Aggregate();
#warning hardcoded statuses
                    if (StatusInfo.Status.NumOfBits == 16)
                    {
                        Info.Add(new KeyValuePair<string, string>("Статус", flags));
                    }
                    else if (StatusInfo.Status.NumOfBits == 32)
                    {
                        Info.Add(new KeyValuePair<string, string>(
                            $"Статус 31÷15",
                            flags.Take(flags.Length / 2).Aggregate()));
                        Info.Add(new KeyValuePair<string, string>(
                            "Статус 15÷00",
                            flags.TakeFromEnd(flags.Length / 2).Aggregate()));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }
    }
}
