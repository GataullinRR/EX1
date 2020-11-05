using WPFUtilities.Types;
using Utilities.Types;
using System.Threading.Tasks;
using Utilities;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Utilities.Extensions;
using MVVMUtilities.Types;
using DeviceBase.Devices;
using System;
using System.Linq;
using DeviceBase;
using Common;
using DeviceBase.IOModels;
using System.Diagnostics;
using System.ComponentModel;
using TinyConfig;
using Calibrators;
using DeviceBase.Models;
using System.Threading;
using RUSTelemetryStreamSenderExports;

namespace RUSTelemetryStreamSenderWidget
{
#warning refactor
    public class RUSTelemetryStreamSenderVM : INotifyPropertyChanged, IDecoratedDataProvider
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("RUSTelemetryStreamSender");
        readonly static ConfigProxy<int> FRAME_DURATION = CONFIG.Read(1000);
        readonly static ConfigProxy<int> PREEMPTIVE_READ_DURATION = CONFIG.Read(3000);

        readonly BusyObject _isBusy;
        RegDataReader _currentData;
        IRUSDevice _device;
        AsyncOperationInfo _operationInfo;
        Task _sendingOperation;
        int _avgSampleRate;

        public event PropertyChangedEventHandler PropertyChanged;
        public event DataRowAquiredDelegate DataRowAquired;
        public event DecoratedDataRowAquiredDelegate DecoratedDataRowAquired;

        public ActionCommand ChooseFile { get; }
        public ActionCommand Start { get; }
        public ActionCommand Stop { get; }
        public RichProgress Progress { get; } = new RichProgress();

        public EnhancedObservableCollection<KeyValuePair<string, string>> Parameters { get; } = new EnhancedObservableCollection<KeyValuePair<string, string>>();

        public RUSTelemetryStreamSenderVM(IRUSDevice device, BusyObject isBusy)
        {
            _isBusy = isBusy ?? throw new ArgumentNullException(nameof(isBusy));
            _device = device ?? throw new ArgumentNullException(nameof(device));

            ChooseFile = new ActionCommand(chooseFileAsync, _isBusy);
            Start = new ActionCommand(async () => 
                {
                    _sendingOperation = startSendingAsync();
                    Stop.Update();  
                    await _sendingOperation;
                },
                () => _currentData != null && _isBusy.IsNotBusy, _isBusy);
            Stop = new ActionCommand(stopSending, () =>_sendingOperation != null, _isBusy);

            async Task stopSending()
            {
                _operationInfo.Cancel();
                await _sendingOperation;
            }

            async Task startSendingAsync()
            {
                using (_isBusy.BusyMode)
                {
                    try
                    {
                        Logger.LogOKEverywhere("Передача начата");

                        _operationInfo = new AsyncOperationInfo(Progress).UseInternalCancellationSource();
                        _operationInfo.Progress.Optimize = false;
                        _operationInfo.Progress.MaxProgress = _currentData.Count;

                        var sw = Stopwatch.StartNew();
                        var sendedCurveTotalDuration = TimeSpan.Zero;
                        for (int i = 0, frameI = 0; i < _currentData.Count ;frameI++)
                        {
                            TimeSpan duration = TimeSpan.Zero;
                            var frame = new List<RegData>();
                            await Task.Run(async () =>
                            {
                                var first = await _currentData[i];
                                frame.Add(first);
                                for (; i < _currentData.Count - 1; i++)
                                {
                                    var item = await _currentData[i + 1];
                                    if (item.Time <= frame.LastElement().Time)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        frame.Add(item);
                                        duration = item.Time - first.Time;
                                        if (duration.TotalMilliseconds >= FRAME_DURATION)
                                        {
                                            i++;
                                            break;
                                        }
                                    }
                                }
                                i++;
                            });
                            if (i == _currentData.Count)
                            {
                                unchecked
                                {
                                    frameI = (int)uint.MaxValue;
                                };

                                Logger.LogOKEverywhere("Отправка файла завершена");
                            }
                            var durationMs = duration.TotalMilliseconds.Round();
                            var frameData = frame.Select(d => d.Row[0]).ToArray();
                            var request = Requests.BuildRegDataFramePacket(frameI, durationMs, frameData);
                            var result = await _device.BurnAsync(Command.REG_DATA_FRAME, request, DeviceOperationScope.DEFAULT, CancellationToken.None);
                            //if (result.Status == BurnRequestStatus.OK)
                            if (true)
                            {
                                var response = result.Response == null 
                                    ? new ulong[0] 
                                    : result.Response.Skip(6).SkipFromEnd(2).GroupBy(8).Select(bs => Deserialize(bs)).ToArray();
                                foreach (var pointI in frameData.Length.Range())
                                {
                                    var entities = new List<ViewDataEntity>();
                                    entities.Add(new ViewDataEntity("Pressure", frameData[pointI], true));
                                    if (response.Length > pointI)
                                    {
                                        entities.Add(new ViewDataEntity($"Response", response[pointI], true));
                                    }
                                    else
                                    {
                                        entities.Add(new ViewDataEntity($"Response", ""));
                                    }

                                    var decoration = (Progress.Progress.Round() + pointI) % _avgSampleRate == 0
                                        ? RowDecoration.LINE
                                        : RowDecoration.NONE;
                                    DecoratedDataRowAquired?.Invoke(entities, decoration);
                                }
                                ulong Deserialize(IEnumerable<byte> data)
                                {
                                    return BitConverter.ToUInt64(data.Take(8).Reverse().ToArray(), 0);
                                }
                                
                                sendedCurveTotalDuration += duration;
                                _operationInfo.Progress.Report(i + 1);

                                await Task.Delay(
                                    (sendedCurveTotalDuration.TotalMilliseconds - sw.Elapsed.TotalMilliseconds - PREEMPTIVE_READ_DURATION).Round().NegativeToZero(),
                                    _operationInfo.CancellationToken);
                            }
                            else
                            {
                                Logger.LogErrorEverywhere("Ошибка передачи кадра");
                                frameI--;
                                i -= frame.Count;
                            }

                            _operationInfo.CancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogInfoEverywhere("Пересылка отменена");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere("Цикл отправки завершился с ошибкой", ex);
                    }
                    finally
                    {
                        _sendingOperation = null;
                        _operationInfo = null;
                        Progress.Reset();
                        Stop.Update();
                    }
                }
            }

            async Task chooseFileAsync()
            {
                using (_isBusy.BusyMode)
                {
                    try
                    {
                        var path = IOUtils.RequestFileOpenPath("RAW (*raw)|*.raw");
                        if (path == null)
                        {
                            Logger.LogInfoEverywhere("Файл не выбран");
                            return;
                        }
                        var file = new FileStream(path, FileMode.Open);
                        _currentData?.Dispose();
                        _currentData = new RegDataReader(file);
                        ChooseFile.Update();
                        using (Parameters.EventSuppressingMode)
                        {
                            Parameters.Clear();
                            var first = await _currentData[0];
                            var last = await _currentData[_currentData.Count - 1];
                            var duration = last.Time - first.Time;
                            _avgSampleRate = (_currentData.Count / duration.TotalSeconds).Round();
                            Parameters.Add(new KeyValuePair<string, string>("Длительность (часов)", duration.TotalHours.Round().ToString()));
                            Parameters.Add(new KeyValuePair<string, string>("Частота семплирования", (_currentData.Count / duration.TotalSeconds).ToStringInvariant("F2")));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere("Ошибка при загрузке", ex);
                        _currentData?.Dispose();
                    }
                }
            }
        }
    }
}
