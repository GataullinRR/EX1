using System.Collections.Generic;
using System.Threading.Tasks;
using MVVMUtilities.Types;
using System.Linq;
using Utilities.Extensions;
using Calibrators;
using DeviceBase.IOModels;
using DeviceBase.Models;
using DeviceBase.Devices;
using Common;
using WPFUtilities.Types;
using System.Threading;
using RUSTelemetryStreamSenderWidget;
using RUSTelemetryStreamSenderExports;

namespace DataRequestWidget
{
    public class DeviceDataRequestVM 
    {
        readonly IRUSDevice _device;
        public BusyObject IsBusy { get; }

        public ActionCommand GetDataRequest { get; }
        public ActionCommand GetDataPacketConfigurationRequest { get; }
        public DeviceDataVM DeviceDataVM { get; }

        public DeviceDataRequestVM(IRUSDevice device, BusyObject isBusy, IDataProvider[] dataProviders)
        {
            _device = device;
            IsBusy = isBusy;

            DeviceDataVM = new DeviceDataVM(_device);

            GetDataRequest = new ActionCommand(getData, IsBusy)
            {
                CanBeExecuted = _device.SupportedCommands.Contains(Command.DATA)
            };
            GetDataPacketConfigurationRequest = new ActionCommand(getDataPacketFormat, IsBusy)
            {
                CanBeExecuted = _device.SupportedCommands.Contains(Command.DATA_PACKET_CONFIGURATION_FILE)
            };

            foreach (var dataProvider in dataProviders)
            {
                if (dataProvider is IDecoratedDataProvider drp)
                {
                    drp.DecoratedDataRowAquired += updateData;
                }
                else
                {
                    dataProvider.DataRowAquired += updateData;
                }
            }

            async Task getDataPacketFormat()
            {
                using (IsBusy.BusyMode)
                {
                    var result = await _device.ReadAsync(Command.DATA_PACKET_CONFIGURATION_FILE, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (result.Status == ReadStatus.OK)
                    {
                        Logger.LogOKEverywhere($"Формат пакета данных обновлен");
                    }
                    else
                    {
                        Logger.LogErrorEverywhere($"Ошибка обновления формата пакета данных");
                    }
                }
            }
            async Task getData()
            {
                using (IsBusy.BusyMode)
                {
                    var data = await _device.ReadAsync(Command.DATA, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (data.Status == ReadStatus.OK)
                    {
                        updateData(data.Entities.GetViewEntities());
                    }
                }
            }
        }

        void updateData(IEnumerable<ViewDataEntity> data)
        {
            DeviceDataVM.Update(data);
        }
        void updateData(IEnumerable<ViewDataEntity> data, RowDecoration decoration)
        {
            DeviceDataVM.Update(data, decoration);
        }
    }
}
