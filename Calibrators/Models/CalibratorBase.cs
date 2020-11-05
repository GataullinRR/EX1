using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.Models;
using MVVMUtilities.Types;
using TinyConfig;
using Utilities.Extensions;

namespace Calibrators.Models
{
    interface ICalibrator : INotifyPropertyChanged
    {
        bool CallibrationCanBeGenerated { get; }
        object CurrentMode { get; }
        IList Results { get; }
        MeasureState State { get; }
        Task<IEnumerable<CalibrationFileEntity>> GenerateCalibrationCoefficientsAsync();
        Task MeasureAsync(object mode, IProgress<double> progress, CancellationToken cancellationToken);
        void RequestFinish();

        bool HasCalibrationBegun { get; }
        Task BeginCalibrationAsync();
        void DiscardCalibration();
    }

    abstract class CalibratorBase : NotifiableObjectTemplate
    {
        protected readonly static ConfigAccessor CONFIG = Configurable
            .CreateConfig("CalibratorBase");
        protected readonly static ConfigProxy<uint> NUMBER_OF_DATA_RECEIVE_ERRORS_BEFORE_STOP = CONFIG.Read((uint)10);
    }

    abstract class CalibratorBase<TMode, TResult, TMnemonicType>
        : CalibratorBase, ICalibrator, IDataProvider
        where TMnemonicType : struct
        where TResult : MeasureResultBase
    {
        protected class KnownEntity
        {
            public MnemonicInfo<TMnemonicType> Info { get; }
            public IDataEntity Entity { get; }
            public double RawValue { get; }
            public double Value { get; }

            public KnownEntity(MnemonicInfo<TMnemonicType> info, IDataEntity entity, double rawValue, double value)
            {
                Info = info;
                Entity = entity;
                RawValue = rawValue;
                Value = value;
            }
        }

        protected class Packet
        {
            public KnownEntity[] Entities { get; }
            public double DTime { get; }
            public double Time { get; }

            public Packet(KnownEntity[] entities, double dTime, double time)
            {
                Entities = entities;
                DTime = dTime;
                Time = time;
            }
        }

        protected readonly IRUSDevice _device;
        protected abstract MnemonicInfo<TMnemonicType>[] _mnemonics { get; }
        bool _finishRequested;

        public event DataRowAquiredDelegate DataRowAquired;

        protected abstract int requestInterval { get; }
        public MeasureState State { get; private set; }
        public abstract EnhancedObservableCollection<TResult> Results { get; }
        IList ICalibrator.Results => Results;
        public abstract bool CallibrationCanBeGenerated { get; }

        public object CurrentMode { get; private set; }
        public bool HasCalibrationBegun { get; private set; }
        public CalibratorBase(IRUSDevice device)
        {
            _device = device;
        }

        /// <summary>
        /// Current measure's received entities
        /// </summary>
        readonly protected List<Packet> _receivedPackets = new List<Packet>();
        CancellationToken _receivingStreamCancellation;

        /// <summary>
        /// Infinite collection which represents data are being aquired from the device
        /// </summary>
        protected IEnumerable<Task<Packet>> receivingStream
        {
            get
            {
                int i = 0;
                for (; true; i++)
                {
                    yield return getNextAsync();
                }

                async Task<Packet> getNextAsync()
                {
                    while (i == _receivedPackets.Count)
                    {
                        await Task.Delay(10, _receivingStreamCancellation);
                    }

                    return _receivedPackets[i];
                }
            }
        }

        /// <summary>
        /// Here you can enumerate <see cref="receivingStream"/> and perform your calibration
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task handleKnownEntitiesLoopAsync(TMode mode, IProgress<double> progress, CancellationToken cancellationToken)
        {

        }

        async Task ICalibrator.MeasureAsync(object mode, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await MeasureAsync((TMode)mode, progress, cancellationToken);
        }
        public async Task MeasureAsync(TMode mode, IProgress<double> progress, CancellationToken cancellationToken)
        {
            CurrentMode = mode;
            await onMeasureAsync(mode);
            _finishRequested = false;
            _receivedPackets.Clear();
            progress.Report(0);
            State = MeasureState.MEASURING;

            {
                var result = await _device.ReadAsync(Command.DATA_PACKET_CONFIGURATION_FILE, DeviceOperationScope.DEFAULT, CancellationToken.None);
                if (result.Status != ReadStatus.OK)
                {
                    State = MeasureState.FINISHED_WITH_ERROR;
                    Logger.LogErrorEverywhere("Не удалось получить формат пакета данных");

                    return;
                }
            }

            var handlingSteramCTS = new CancellationTokenSource();
            cancellationToken.Register(() => handlingSteramCTS.Cancel());
            _receivingStreamCancellation = handlingSteramCTS.Token;
            var handlingTask = handleKnownEntitiesLoopAsync(mode, progress, cancellationToken);

            try
            {
                var measureStartDateTime = DateTime.Now;
                var numberOfDataReceiveErrors = 0;
                var sw = Stopwatch.StartNew();
                while (true)
                {
                    if (handlingTask.Status == TaskStatus.Faulted)
                    {
                        handlingTask.GetAwaiter().GetResult(); // rethrow
                    }

                    await onSingleMeasureAsync(mode);
                    await Task.Delay(requestInterval, cancellationToken);

                    if (_finishRequested)
                    {
                        break;
                    }

                    var readRequestResult = await _device.ReadAsync(Command.DATA, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    if (readRequestResult.Status != ReadStatus.OK)
                    {
                        numberOfDataReceiveErrors++;
                        Logger.LogWarningEverywhere($"Ошибка запроса данных {numberOfDataReceiveErrors}/{NUMBER_OF_DATA_RECEIVE_ERRORS_BEFORE_STOP}");
                        if (numberOfDataReceiveErrors >= NUMBER_OF_DATA_RECEIVE_ERRORS_BEFORE_STOP)
                        {
                            throw new Exception("Превышение допустимого количества ошибок приема данных");
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // If (cal mode was set) and it is required to get data packet again and check its status field again
                        if (await ensureInCalibrationModeAsync(readRequestResult))
                        {
                            State = MeasureState.WAITING_FOR_CALIBRATION_MODE_SET;

                            continue;
                        }
                        else if (State == MeasureState.WAITING_FOR_CALIBRATION_MODE_SET) // Next iteration of the first if clause
                        {
                            State = MeasureState.MEASURING;

                            Logger.LogInfo(null, "Режим калибровки установлен");
                        }

                        var knownEntitiesReceived = handleKnownEntities(readRequestResult).ToArray();
                        var t = sw.Elapsed.TotalMilliseconds;
                        var dt = t - (_receivedPackets.LastElementOrDefault()?.Time ?? 0D);
                        _receivedPackets.Add(new Packet(knownEntitiesReceived, dt, t));

                        await handleDataPacketAsync(mode, progress, knownEntitiesReceived);
                        provideData(readRequestResult, knownEntitiesReceived);
                    }
                }

                await finishAsync(mode, measureStartDateTime);
                State = MeasureState.FINISHED_SUCCESSFULLY;
            }
            catch (OperationCanceledException)
            {
                State = MeasureState.CANCELLED;
                throw;
            }
            catch (Exception)
            {
                State = MeasureState.FINISHED_WITH_ERROR;
                handlingSteramCTS.Cancel();
                throw;
            }
            finally
            {
                try
                {
                    await tryQuitCalibrationModeAsync(cancellationToken);
                    await handlingTask;
                }
                finally
                {
                    onMeasureFinallized(mode);
                }
            }

            IEnumerable<KnownEntity> handleKnownEntities(ReadResult readRequestResult)
            {
                foreach (var mi in _mnemonics.Length.Range())
                {
                    var m = _mnemonics[mi];
                    var entity = readRequestResult.Entities.Find(e => e.Descriptor.Name == m.DeviceMnemonic);
                    if (entity.Found)
                    {
                        if (m.IsInvalidValue(entity.Value.Value))
                        {
                            Logger.LogErrorEverywhere($"Недопустимое значение \"{m.DeviceMnemonic}\"");

                            throw new Exception($"Недопустимое значение мнемоники \"{m.DeviceMnemonic}\". Значение: {entity.Value.Value}");
                        }
                        else
                        {
                            var rawValue = (double)Convert.ChangeType(entity.Value.Value, TypeCode.Double);

                            yield return new KnownEntity(m, entity.Value, rawValue, m.GetValue(entity.Value.Value));
                        }
                    }
                    else
                    {
                        Logger.LogErrorEverywhere($"Отсутствует мнемоника \"{m.DeviceMnemonic}\"");

                        throw new Exception($"В пакете отсустсвует мнемоника \"{m.DeviceMnemonic}\"");
                    }
                }
            }
            void provideData(
                ReadResult readRequestResult,
                IEnumerable<KnownEntity> entitiesReceived)
            {
                var fullList = readRequestResult.Entities.Select(
                    e => (e, entitiesReceived.Find(ke => ke.Entity.Descriptor == e.Descriptor).ValueOrDefault));
                var newData = transformAquiredData(fullList);

                DataRowAquired?.Invoke(newData);
            }
        }
        protected virtual IEnumerable<ViewDataEntity> transformAquiredData(
            IEnumerable<(IDataEntity Entity, KnownEntity KnownEntity)> entitiesReceived)
        {
            foreach (var info in entitiesReceived)
            {
                if (info.KnownEntity == null)
                {
                    var point = (info.Entity as IPointDataEntity)?.Point ?? (double?)null;
                    if (point.HasValue)
                    {
                        yield return new ViewDataEntity(
                            info.Entity.Descriptor.Name, 
                            point.Value,
                            info.Entity.Descriptor.ValueFormat.IsInteger());
                    }
                    else
                    {
                        yield return new ViewDataEntity(
                            info.Entity.Descriptor.Name, 
                            info.Entity.Descriptor.SerializeToString(info.Entity.Value));
                    }
                }
                else
                {
                    yield return new ViewDataEntity(
                        info.Entity.Descriptor.Name, 
                        info.KnownEntity.Value,
                        false);
                }
            }
        }

        protected virtual async Task tryQuitCalibrationModeAsync(CancellationToken token)
        {

        }
        protected virtual bool isInCalibrationMode(object status)
        {
            return false;
        }
        protected virtual async Task<bool> ensureInCalibrationModeAsync(ReadResult readRequestResult)
        {
            return false;
        }
        protected virtual async Task onSingleMeasureAsync(TMode mode) { }
        protected virtual async Task onMeasureAsync(TMode mode) { }
        protected virtual void onMeasureFinallized(TMode mode) { }
        protected virtual async Task finishAsync(TMode mode, DateTime measureStart) { }
        protected virtual async Task handleDataPacketAsync(TMode mode,
            IProgress<double> progress,
            IEnumerable<KnownEntity> entities)
        { }
        protected void setWaitingForFinish()
        {
            if (State == MeasureState.MEASURING)
            {
                State = MeasureState.WAITING_FOR_FINISH;
            }
        }

        public void RequestFinish()
        {
            if (State == MeasureState.WAITING_FOR_FINISH)
            {
                _finishRequested = true;
            }
        }
        /// <summary>
        /// Doesn't look at the <see cref="State"/>
        /// </summary>
        protected void forceRequestFinish()
        {
            _finishRequested = true;
        }

        public abstract Task<IEnumerable<CalibrationFileEntity>> GenerateCalibrationCoefficientsAsync();

        public async Task BeginCalibrationAsync()
        {
            if (!HasCalibrationBegun)
            {
                await beginCalibrationAsync();
                HasCalibrationBegun = true;
            }
        }
        protected virtual Task beginCalibrationAsync() => Task.FromResult(true);
        public void DiscardCalibration()
        {
            if (HasCalibrationBegun)
            {
                discardCalibration();
                State = default;
                _receivedPackets.Clear();
                HasCalibrationBegun = false;
            }
        }
        protected abstract void discardCalibration();
    }
}