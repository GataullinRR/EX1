using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Vectors;
using DeviceBase;
using MVVMUtilities.Types;
using TinyConfig;
using SpecificMarshallers;
using DeviceBase.IOModels;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using System.ComponentModel;
using System.Collections;
using Common;

namespace Calibrators.Models
{
    public enum MeasureState
    {
        WAITING_FOR_START = 0,
        FINISHED_WITH_ERROR,
        FINISHED_SUCCESSFULLY,
        CANCELLED,
        WAITING_FOR_CALIBRATION_MODE_SET,
        WAITING_FOR_FINISH,
        MEASURING
    }

    interface IInclinometrCalibrator : INotifyPropertyChanged, ICalibrator
    {
        CalibrationConstants Constants { get; }
    }

    internal enum IncMnemonicType
    {
        INC,
        AZI,
        GTF,
        GX,
        GY,
        GZ,
        BX,
        BY,
        BZ,
        ACCELEROMETR_TEMPERATURE,
        MAGNITOMETR_TEMPERATURE,
    }

    abstract class InclinometrCalibratorBase<TMode, TResult> 
        : CalibratorBase<TMode, TResult, IncMnemonicType>, IInclinometrCalibrator
        where TResult : MeasureResultBase
    {
        const string STATUS_MNEMONIC = "STAT";

        protected override MnemonicInfo<IncMnemonicType>[] _mnemonics { get; }  = new MnemonicInfo<IncMnemonicType>[]
        {
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.INC, "INC_", x => x == ushort.MaxValue, x => x * 180.0 / 65534, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.AZI, "AZI_", x => x == ushort.MaxValue, x => x * 360.0 / 65534, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.GTF, "GTF_", x => x == ushort.MaxValue, x => x * 360.0 / 65534, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.GX, "XGpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.GY, "YGpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.GZ, "ZGpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.BX, "XMpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.BY, "YMpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.BZ, "ZMpr", x => x == short.MaxValue, x => x / 10000.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.ACCELEROMETR_TEMPERATURE, "TGad", x => x == short.MaxValue, x => x / 10.0, x => x),
            new MnemonicInfo<IncMnemonicType>(IncMnemonicType.MAGNITOMETR_TEMPERATURE, "TMad", x => x == short.MaxValue, x => x / 10.0, x => x)
        };

        protected abstract InclinometrMode modeDuringCalibration { get; }
        public CalibrationConstants Constants { get; } = new CalibrationConstants()
        {
            DipAngle = 72.1,
            BTotal = 0.556
        };

        public InclinometrCalibratorBase(IRUSDevice device)
            : base(device)
        {

        }

        protected override async Task tryQuitCalibrationModeAsync(CancellationToken cancellation)
        {
            Logger.LogInfoEverywhere("Установка рабочего режима");

            var data = Requests.BuildInclinometrModeSetPacket(InclinometrMode.WORKING);
            await _device.BurnAsync(Command.CALIBRATION_MODE_SET, data, DeviceOperationScope.DEFAULT, cancellation);
            var statusReadResult = await _device.TryReadStatusAsync(DeviceOperationScope.DEFAULT, cancellation);
            var isInWorkingMode = statusReadResult == null
                ? false
                : !isInCalibrationMode(statusReadResult.StatusInfo.Status.Bits);

            if (!isInWorkingMode)
            {
                Logger.LogErrorEverywhere("Не удалось выйти из режима калибровки");
            }
        }
        protected override async Task<bool> ensureInCalibrationModeAsync(ReadResult readRequestResult)
        {
            {
                var status = readRequestResult.Entities.Find(e => e.Descriptor.Name == STATUS_MNEMONIC);
                if (status.Found)
                {
                    var inCalibrationMode = isInCalibrationMode(status.Value.Value);
                    if (!inCalibrationMode)
                    {
                        Logger.LogInfo(null, $"Установка режима {modeDuringCalibration}");

                        var data = Requests.BuildInclinometrModeSetPacket(modeDuringCalibration);
                        await _device.BurnAsync(Command.CALIBRATION_MODE_SET, data, DeviceOperationScope.DEFAULT, CancellationToken.None);
                    }

                    return !inCalibrationMode;
                }
                else
                {
                    var msg = $"Мнемоника статуса \"{STATUS_MNEMONIC}\" не найдена в пакете данных";
                    Logger.LogErrorEverywhere($"Отсутствует мнемоника \"{STATUS_MNEMONIC}\"");

                    throw new Exception(msg);
                }
            }

            bool isInCalibrationMode(object status)
            {
                var status16 = (ushort)Convert.ChangeType(status, TypeCode.UInt16);
                var incMode = new DeviceStatusInfo(_device.Id, 0, new DeviceStatusInfo.StatusInfo(16, status16))
                    .InclinometrMode;
                if (incMode == null)
                {
                    Logger.LogWarningEverywhere($"Некорректный режим работы инклинометра. Статус: {status16:X4}");
                }

                return incMode == modeDuringCalibration;
            }
        }

        public override async Task<IEnumerable<CalibrationFileEntity>> GenerateCalibrationCoefficientsAsync()
        {
            await ThreadingUtils.ContinueAtThreadPull();

            var calibrationCoefficients = new List<CalibrationFileEntity>();
            if (CallibrationCanBeGenerated)
            {
                var calibrator = new CalibratorApplication(Results, Constants);
                var coefficients = await calibrator.CalculateCoefficientsAsync();

                return await generateCalibrationCoefficients(coefficients);
            }
            else
            {
                throw new InvalidOperationException("Необходимые замеры не были произведены");
            }
        }
        protected abstract Task<IEnumerable<CalibrationFileEntity>> generateCalibrationCoefficients(IEnumerable<Curve> curves);
    }
}