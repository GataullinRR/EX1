using CommandExports;
using Common;
using Controls;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;
using WPFUtilities.Types;

namespace CommandWidget
{
    public class DeviceCommandVM : INotifyPropertyChanged
    {
        readonly IRUSDevice _device;
        readonly Command _request;
        AsyncOperationInfo _currentRequest;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; }
        public EnhancedObservableCollection<CommandEntityVM> Entities { get; } = new EnhancedObservableCollection<CommandEntityVM>();
        public ICommandHandlerWidget Widget { get; }
        public BusyObject IsBusy { get; }
        public bool IsReadSupported { get; }
        public bool IsSendSupported { get; }
        public ActionCommand Read { get; }
        public ActionCommand Send { get; }
        public ActionCommand Cancel { get; }
        public RichProgress Progress { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="commandAddress"></param>
        /// <param name="handlerWidget">optional</param>
        /// <param name="isBusy"></param>
        public DeviceCommandVM(IRUSDevice device, Command commandAddress, ICommandHandlerWidget handlerWidget, BusyObject isBusy)
        {
            _device = device;
            _request = commandAddress;
            IsBusy = isBusy;
            Widget = handlerWidget;

            var requestinfo = Requests.GetRequestDescription(_device.Id, _request);
            Entities.AddRange(requestinfo.UserCommandDescriptors
                .Select(d => new CommandEntityVM(d))
                .ToArray());
            Name = _request.GetInfo().CommandName;
            Send = new ActionCommand(burnAsync, IsBusy);
            Read = new ActionCommand(readAsync, IsBusy);
            Cancel = new ActionCommand(cancelAsync, () => _currentRequest != null, IsBusy);
            Read.CanBeExecuted = IsReadSupported = commandAddress.GetInfo().IsReadSupported;
            Send.CanBeExecuted = IsSendSupported = commandAddress.GetInfo().IsWriteSupported;
            Cancel.CanBeExecuted = Widget?.Settings?.AllowCancelling ?? false;
            Progress = (Widget?.Settings?.ShowProgress ?? false)
                ? new RichProgress()
                : null;

            async Task burnAsync()
            {
                using (IsBusy.BusyMode)
                {
                    Logger.LogInfoEverywhere($"Отправка комманды записи \"{Name}\"");
                    
                    if (Widget != null)
                    {
                        throw new NotImplementedException();
                    }

                    assertViewValues();
                    var entitiesToWrite = Entities.Select(e => e.Entity).ToArray();
                    var allEntitiesToWrite = requestinfo.BuildWriteRequestBody(entitiesToWrite);
                    var result = await _device.BurnAsync(_request, allEntitiesToWrite, DeviceOperationScope.DEFAULT, CancellationToken.None);

                    if (result.Status == BurnStatus.OK)
                    {
                        if (IsReadSupported)
                        {
                            await verifyBurnAsync(allEntitiesToWrite);
                        }
                        else
                        {
                            Logger.LogOKEverywhere("Запись завершена");
                        }
                    }
                    else
                    {
                        Logger.LogErrorEverywhere("Ошибка отправки команды");
                    }
                }

                async Task verifyBurnAsync(IEnumerable<IDataEntity> allEntitiesToWrite)
                {
                    var readEntities = await readAsync();
                    if (readEntities != null)
                    {
                        var isVerified = readEntities
                            .Select(e => e.Value)
                            .SequenceEqual(allEntitiesToWrite.Select(e => e.Value));
                        if (isVerified)
                        {
                            Logger.LogOKEverywhere("Запись завершена");
                        }
                        else
                        {
                            Logger.LogWarningEverywhere("Записанные данные не совпадают с прочитанными");
                        }
                    }
                }
            }

            async Task<IEnumerable<IDataEntity>> readAsync()
            {
                using (IsBusy.BusyMode)
                {
                    Logger.LogInfoEverywhere($"Отправка комманды чтения \"{Name}\"");

                    if (Widget != null)
                    {
                        await widgetsHandlerAsync();

                        return null;
                    }
                    else
                    {
                        var result = await _device.ReadAsync(_request, DeviceOperationScope.DEFAULT, CancellationToken.None);
                        if (result.Status == ReadStatus.OK)
                        {
                            foreach (var entity in Entities)
                            {
                                var match = result.Entities.First(e => e.Descriptor.Equals(entity.Descriptor));
                                entity.EntityValue.ModelValue = match.Value;
                            }
                            assertViewValues();

                            Logger.LogOKEverywhere("Чтение завершено");

                            return result.Entities;
                        }
                        else
                        {
                            Logger.LogErrorEverywhere("Ошибка отправки команды");

                            return null;
                        }
                    }
                }

                async Task widgetsHandlerAsync()
                {
                    try
                    {
                        Progress.Reset();
                        _currentRequest = new AsyncOperationInfo(Progress).UseInternalCancellationSource();
                        Cancel.Update();
                        await Widget.Model.OnReadAsync(_currentRequest);
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogInfoEverywhere("Команда была отменена");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere("Ошибка отправки команды", ex);
                    }
                    finally
                    {
                        _currentRequest = null;
                        Cancel.Update();
                    }
                }
            }

            async Task cancelAsync()
            {
                _currentRequest.Cancel();
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
}
