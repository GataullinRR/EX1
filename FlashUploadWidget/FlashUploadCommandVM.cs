using Common;
using DeviceBase.Devices;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using WPFUtilities.Types;
using DeviceBase.IOModels;
using DeviceBase.Helpers;
using System.IO;
using CommandExports;
using FlashDumpLoaderExports;
using System.ComponentModel;
using System.Collections.ObjectModel;
using WPFControls;
using DeviceBase;

namespace FlashUploadWidget
{
    public class FlashUploadCommandVM : ICommandHandlerModel
    {
        enum FormatSource
        {
            [Description("Модуля")]
            MODULE,
            [Description("Плат")]
            DEVICES
        }

        readonly IRUSDevice _device;
        readonly IFlashDumpDataParserFactory _parserFactory;
        readonly IFlashDumpSaver _flashDumpSaver;
        FlashStreamReader _dump;

        public BusyObject IsBusy { get; }
        public ActionCommand SaveDump { get; }
        public ObservableCollection<BoolOption> DataFormatSource { get; } = new ObservableCollection<BoolOption>()
        {
            new BoolOption(){ OptionName = FormatSource.MODULE.GetEnumValueDescription(), Tag = FormatSource.MODULE, IsChecked = true},
            new BoolOption(){ OptionName = FormatSource.DEVICES.GetEnumValueDescription(), Tag = FormatSource.DEVICES},
        };
        FormatSource formatSource => DataFormatSource.First(o => o.IsChecked).Tag.To<FormatSource>();

        public FlashUploadCommandVM(IRUSDevice device, IFlashDumpDataParserFactory parserFactory, IFlashDumpSaver flashDumpSaver, IFlashDumpLoader dumpLoader, BusyObject busy)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _parserFactory = parserFactory;
            _flashDumpSaver = flashDumpSaver;
            IsBusy = busy ?? throw new ArgumentNullException(nameof(busy));

            SaveDump = new ActionCommand(saveDumpAsync, () => _dump != null && IsBusy.IsNotBusy, IsBusy);

            async Task saveDumpAsync()
            {
                try
                {
                    var path = IOUtils.RequestFileSavingPath("BIN(*.bin)|*.bin");
                    if (path != null)
                    {
                        using (var targetDumpFile = File.Create(path))
                        {
                            await _dump.SaveDumpAsync(targetDumpFile, _flashDumpSaver, new AsyncOperationInfo());
                        }

                        await dumpLoader.LoadAsync(path, new AsyncOperationInfo());

                        UserInteracting.ReportSuccess("Сохранение дампа Flash-памяти", "Файл успешно сохранен");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogErrorEverywhere("Ошибка сохранения дампа Flash-памяти", ex);
                }
            }
        }

        public Task OnBurnAsync(AsyncOperationInfo operationInfo)
        {
            throw new NotSupportedException();
        }

        public async Task OnReadAsync(AsyncOperationInfo operationInfo)
        {
            if (_dump != null && !await UserInteracting.RequestAcknowledgementAsync("Начать загрузку дампа Flash-памяти", "Предыдущий дамп будет стерт из памяти программы-NL-NLПродолжить?"))
            {
                return;
            }

            operationInfo.Progress.Report(0);
            var formats = new Dictionary<RUSDeviceId, IEnumerable<IDataEntity>>();
            switch (formatSource)
            {
                case FormatSource.MODULE:
                    {
                        var format = await _device.ReadAsync(Command.DATA_PACKET_CONFIGURATION_FILE, DeviceOperationScope.DEFAULT, operationInfo);
                        if (format.Status != ReadStatus.OK)
                        {
                            Logger.LogErrorEverywhere("Не удалось получить формат данных");

                            return;
                        }
                        formats.Add(_device.Id, format.Entities);
                    }
                    break;

                case FormatSource.DEVICES:
                    {
                        foreach (var device in _device.Children.Concat(_device))
                        {
                            var format = await device.ReadAsync(Command.FLASH_DATA_PACKET_CONFIGURATION, DeviceOperationScope.DEFAULT, operationInfo);
                            if (format.Status == ReadStatus.OK)
                            {
                                formats.Add(device.Id, format.Entities);
                            }
                            else
                            {
                                Logger.LogWarningEverywhere($"Не удалось получить формат данных для устройства: {device.Id}");
                            }
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (formats.Count == 0)
            {
                Logger.LogErrorEverywhere($"Ни один из требуемых форматов не был получен");

                return;
            }

            var reader = await FlashStreamReader.CreateAsync(_device.Id, formats, _parserFactory, operationInfo);
            var result = await _device.ReadAsync(Command.DOWNLOAD_FLASH, reader.FlashReadOperationScope, operationInfo);
            if (result.Status == ReadStatus.OK)
            {
                await reader.FinishAsync(operationInfo);
                _dump = reader;
            }
        }
    }
}