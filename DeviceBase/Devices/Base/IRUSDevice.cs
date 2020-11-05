using DeviceBase.IOModels;
using DeviceBase.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.Devices
{
    public enum RUSDeviceId : byte
    {
        /// <summary>
        /// Broadcast
        /// </summary>
        ALL = 0,

        [Description("Драйвер привода")]
        DRIVE_CONTROL = RUSDriveControll.ID,
        [Description("Инклинометр")]
        INCLINOMETR = RUSInclinometr.ID,
        [Description("Измеритель")]
        IZMERITEL = RUSIzmeritel.ID,
        [Description("Блок привязки LWD")]
        LWD_LINK = RUSLWDLink.ID,
        [Description("Датчик вращения")]
        ROTATIONS_SENSOR = RUSRotationSensor.ID,
        [Description("Датчик ударов")]
        SHOCK_SENSOR = RUSShockSensor.ID,
        [Description("Плата телеметрии телесистемы")]
        TELEMETRY = RUSTelemetry.ID,
        [Description("Модем телесистемы")]
        TELESYSTEM = RUSTelesystem.ID,

        [Description("Модуль ЭМК")]
        EMC_MODULE = RUSEMCModule.ID,
        [Description("Модуль РУС")]
        RUS_MODULE = RUSModule.ID,
        [Description("Технолог. модуль LWD")]
        RUS_TECHNOLOGICAL_MODULE = RUSTechnologicalModule.ID,
    }

    /// <summary>
    /// Keep in mind: async operations are run on different threads, so do not access any passed in object till current operation finishes!
    /// Actual for V14.0.0
    /// </summary>
    public interface IRUSDevice
    {
        RUSDeviceId Id { get; }
        string Name { get; }

        IReadOnlyList<Command> SupportedCommands { get; }
        IReadOnlyList<IRUSDevice> Children { get; }

        Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation);
        Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation);
        Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation);

        Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation);
        Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation);

        T TryGetFeature<T>() where T : class, IRUSDeviceFeature;
    }
}
