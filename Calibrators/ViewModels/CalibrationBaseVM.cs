using Common;
using Calibrators.Models;
using DeviceBase;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using WPFUtilities.Types;
using MVVMUtilities;

namespace Calibrators.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TCalibrator"></typeparam>
    /// <typeparam name="TMode">Can be of any type</typeparam>
    internal abstract class CalibrationBaseVM<TCalibrator, TMode> : INotifyPropertyChanged, ICalibratorModel
        where TCalibrator : Models.ICalibrator
    {
        public class Preferences
        {
            public bool RequestCancellationAcknowledgement { get; set; } = true;
        }

        readonly CalibrationFileGenerator _fileGenerator;
        readonly Func<FileType, IEnumerable<IDataEntity>, Task> _saveCalibrationFileAsync;
        Task _measureProcess;
        CancellationTokenSource _measureCTS = new CancellationTokenSource();

        protected TCalibrator _calibrator { get; }
        protected IRUSDevice _device { get; }
        protected Preferences _preferences { get; } = new Preferences();

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract IRichProgress MeasureProgress { get; }
        protected abstract bool canExport { get; }
        protected abstract MeasureResultBase exportingResult { get; }
        protected abstract FileType calibrationFileType { get; }

        /// <summary>
        /// Mode can be selected through this command's parameter
        /// </summary>
        public ActionCommand<TMode> StartMeasure { get; }
        public ActionCommand CancelMeasure { get; }
        public ActionCommand<TMode> FinishMeasure { get; }
        public ActionCommand ExportMeasureResults { get; }
        public ActionCommand ImportMeasureResults { get; }
        public ActionCommand SaveCalibrationFile { get; }
        public ActionCommand Begin { get; }
        public ActionCommand Discard { get; }

        public virtual EnhancedObservableCollection<TMode> Modes { get; } = null;
        public virtual TMode SelectedMode { get; protected set; }

        public string Status { get; protected set; }
        public BusyObject IsBusy { get; }

        public RUSDeviceId TargetDeviceId => _device.Id;
        public abstract string CalibrationName { get; }
        public IDataProvider DataProvider { get; }

        public bool HasCalibrationBegun => _calibrator.HasCalibrationBegun;

        public CalibrationBaseVM(
            BusyObject busy,
            IRUSDevice device,
            Func<FileType, IEnumerable<IDataEntity>, Task> saveCalibrationFileAsync)
        {
            _device = device;
            _calibrator = instantiateCalibrator();
            DataProvider = _calibrator is IDataProvider
                ? (IDataProvider)_calibrator
                : (IDataProvider)(_device = new RUSDeviceDataProviderProxy(_device));
            _calibrator.RedirectAnyChangesTo(this, () => PropertyChanged, nameof(HasCalibrationBegun));
            IsBusy = busy;
            _fileGenerator = new CalibrationFileGenerator(device, calibrationFileType, "01");
            _saveCalibrationFileAsync = saveCalibrationFileAsync;
            _calibrator.PropertyChanged += (o, e) => updateModelBindings();
            updateModelBindings();

            StartMeasure = new ActionCommand<TMode>(startMeasureAsync,
                m => canStartMeasure(m) && !IsBusy,
                IsBusy, this, _calibrator);
            CancelMeasure = new ActionCommand(cancelMeasureAsync);
            FinishMeasure = new ActionCommand<TMode>(finishMeasureAsync,
                m => canFinishMeasure(m)
                    && _calibrator.State == MeasureState.WAITING_FOR_FINISH
                    && (!m.IsSet || Equals(getModelsTestMode(m.Value), _calibrator.CurrentMode)),
                _calibrator);
            ExportMeasureResults = new ActionCommand(exportMeasureResultAsync,
                () => !IsBusy && canExport,
                this, _calibrator, IsBusy);
            ImportMeasureResults = new ActionCommand(importMeasureResultAsync, IsBusy);
            SaveCalibrationFile = new ActionCommand(saveCalibrationsAsync,
                () => !IsBusy && _calibrator.CallibrationCanBeGenerated,
                IsBusy, _calibrator);
            CancelMeasure.CanBeExecuted = false;
            Begin = new ActionCommand(beginAsync,
                () => !HasCalibrationBegun && !IsBusy,
                IsBusy, _calibrator);
            Discard = new ActionCommand(discardAsync,
                () => HasCalibrationBegun && !IsBusy,
                IsBusy, _calibrator);
        }

        protected virtual bool canBegin => true;
        protected virtual bool canDiscard => true;
        protected virtual async Task beginAsync()
        {
            onBegin();
            await _calibrator.BeginCalibrationAsync();
        }
        protected virtual void onBegin() { }
        protected virtual async Task discardAsync()
        {
            if (UserInteracting.RequestAcknowledgement("Отмена калибровки", "Данные проведенных замеров будут стерты. -NL-NLПродолжить?"))
            {
                onDiscard();
                await cancelMeasureAnywayAsync();
                _calibrator.DiscardCalibration();
            }
        }
        protected virtual void onDiscard() { }

        protected abstract TCalibrator instantiateCalibrator();

        /// <summary>
        /// Is used in case if <typeparamref name="TCalibrator"/> mode has diferent type from <typeparamref name="TMode"/>
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        protected virtual object getModelsTestMode(TMode mode)
        {
            return mode;
        }

        async Task startMeasureAsync(CommandParameter<TMode> mode)
        {
            using (IsBusy.BusyMode)
            {
                // In command-parameter driven mode select mode
                if (mode.IsSet)
                {
                    if (Modes?.Contains(mode.Value) ?? true)
                    {
                        SelectedMode = mode.Value;
                    }
                    else 
                    {
                        throw new NotSupportedException();
                    }
                }

                beforeMeasureStart();

                Logger.LogInfoEverywhere($"Запуск замера {SelectedMode}");

                CancelMeasure.CanBeExecuted = true;
                StartMeasure.CanBeExecuted = false;
                MeasureProgress.Report(0);

                _measureCTS = new CancellationTokenSource();
                try
                {
                    _measureProcess = _calibrator.MeasureAsync(getModelsTestMode(SelectedMode), MeasureProgress, _measureCTS.Token);
                    await _measureProcess;
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInfoEverywhere($"Замер прерван");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Ошибка при замере", "Ошибка при замере во время калибровки", ex);
                }

                onMeasureFinished();

                await cancelMeasureAnywayAsync();
            }
        }
        protected virtual void onMeasureFinished() { }
        protected virtual void beforeMeasureStart() { }
        protected virtual bool canStartMeasure(CommandParameter<TMode> mode) => true;
        protected virtual bool canFinishMeasure(CommandParameter<TMode> mode) => true;

        async Task cancelMeasureAsync()
        {
            using (IsBusy.BusyMode)
            {
                if (!_preferences.RequestCancellationAcknowledgement
                    || UserInteracting.RequestAcknowledgement("Прервать замер", "Данные текущего замера будут потеряны.-NL-NLПродолжить?"))
                {
                    await cancelMeasureAnywayAsync();
                }
            }
        }
        async Task cancelMeasureAnywayAsync()
        {
            using (IsBusy.BusyMode)
            {
                CancelMeasure.CanBeExecuted = false;
                StartMeasure.CanBeExecuted = true;

                if (!_measureCTS.IsCancellationRequested)
                {
                    _measureCTS.Cancel();
                }
                await (_measureProcess ?? Task.FromResult(true)).CatchAnyExeption();
            }
        }

        async Task finishMeasureAsync(CommandParameter<TMode> mode)
        {
            using (IsBusy.BusyMode)
            {
                if (mode.IsSet)
                {
                    if (!Equals(_calibrator.CurrentMode, getModelsTestMode(mode.Value)))
                    {
                        throw new InvalidOperationException();
                    }
                }

                _calibrator.RequestFinish();
                await (_measureProcess ?? Task.FromResult(true)).CatchAnyExeption();
            }
        }

        async Task exportMeasureResultAsync()
        {
            using (IsBusy.BusyMode)
            {
                var path = IOUtils.RequestFolderPath();
                if (path != null)
                {
                    Logger.LogInfoEverywhere($"Сохранение замеров {SelectedMode}");

                    var result = await exportingResult.SerializeAsync();
                    var filePath = Path.Combine(path, result.FileName);
                    using (var file = IOUtils.TryCreateFileOrNull(filePath))
                    {
                        if (file == null)
                        {
                            Logger.LogErrorEverywhere($"Ошибка сохранения замеров");
                            Reporter.ReportError($"Не удалось создать файл. Возможно он используется другой программой или программа не имеет доступа на запись в данную директорию." +
                                $"{Global.NL}Путь к файлу: {filePath}");
                        }
                        else
                        {
                            await file.WriteAsync(result.Content);
                        }
                    }
                }
            }
        }

        async Task importMeasureResultAsync()
        {
            using (IsBusy.BusyMode)
            {
                var path = IOUtils.RequestFileOpenPath("CSV (*.csv)|*.csv");
                if (path != null)
                {
                    Logger.LogInfoEverywhere($"Загрузка замеров");

                    var fileData = await CommonUtils.TryOrDefaultAsync(() => File.ReadAllBytes(path));
                    if (fileData == null)
                    {
                        Logger.LogErrorEverywhere($"Ошибка загрузки");
                        Reporter.ReportError("Не удалось открыть файл." +
                                $"{Global.NL}Путь к файлу: {path}");
                    }
                    else
                    {
                        try
                        {
                            await loadResultAsync(Path.GetFileNameWithoutExtension(path), fileData);

                            Logger.LogOKEverywhere($"Замеры загружены");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorEverywhere($"Ошибка загрузки замеров");
                            Reporter.ReportError("Не удалось прочитать файл." +
                                $"{Global.NL}Путь к файлу: {path}", ex);
                        }
                    }
                }
            }
        }
        protected abstract Task loadResultAsync(string fileName, byte[] fileData);

        async Task saveCalibrationsAsync()
        {
            using (IsBusy.BusyMode)
            {
                var coefficients = (await _calibrator
                    .GenerateCalibrationCoefficientsAsync()
                    .CatchAnyExeptionOrDefault())
                    ?.ToArray();
                if (coefficients == null)
                {
                    Logger.LogErrorEverywhere($"Не удалось сгенерировать файл калибровок");
                    return;
                }
                else
                {
                    try
                    {
                        var file = await _fileGenerator.GenerateAsync(coefficients);
                        file = Files.SetBurnDate(file, DateTime.UtcNow);
                        await _saveCalibrationFileAsync(calibrationFileType, file);

                        await onCalibrationsSavedAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere($"Ошибка при построении или сохранении файла", ex);
                    }
                }
            }
        }
        protected virtual async Task onCalibrationsSavedAsync()
        {

        }

        protected virtual void updateModelBindings()
        {
            Status = _calibrator.State.ToString(
                s => s.ToString(),
                (MeasureState.WAITING_FOR_START, "ожидание запуска"),
                (MeasureState.MEASURING, "замер"),
                (MeasureState.WAITING_FOR_FINISH, "ожидание завершения"),
                (MeasureState.WAITING_FOR_CALIBRATION_MODE_SET, "установка режима"),
                (MeasureState.FINISHED_SUCCESSFULLY, "замер успешно завершен"),
                (MeasureState.FINISHED_WITH_ERROR, "ошибка во время замера"),
                (MeasureState.CANCELLED, "замер прерван"));
        }
    }
}
