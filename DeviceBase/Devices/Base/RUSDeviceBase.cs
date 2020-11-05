using Common;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyConfig;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    abstract class RUSDeviceBase : IRUSDevice
    {
        const int DELAY_BEFORE_FILE_BURN_VERIFICATION = 500;

        IEnumerable<EntityDescriptor> _dataPacketDescriptors;
        readonly protected IRUSConnectionInterface _pipe;

        public abstract RUSDeviceId Id { get; }
        public string Name { get; }
        public IReadOnlyList<Command> SupportedCommands { get; }
        public IReadOnlyList<IRUSDevice> Children { get; }

        public RUSDeviceBase(MiddlewaredConnectionInterfaceDecorator pipe)
            : this(pipe, new IRUSDevice[0])
        {

        }
        public RUSDeviceBase(MiddlewaredConnectionInterfaceDecorator pipe, IReadOnlyList<IRUSDevice> children)
        {
            _pipe = pipe;
            Children = children ?? throw new ArgumentNullException(nameof(children));
            Name = Id.GetEnumValueDescription();

            SupportedCommands = Files.Descriptors.Keys
                .Where(t => t.TargetDeviceId == Id)
                .Select(k => k.FileType.GetRequestAddress())
                .Concat(EnumUtils
                    .GetValues<Command>()
                    .Select(a => (Addr: a, Info: a.GetInfo()))
                    .Where(i => i.Info != null && 
                                !i.Info.IsFileRequest &&
                                _pipe.SupportedProtocols.Contains(i.Info.Protocol) &&
                                i.Info.IsSupportedForDevice(Id))
                    .Select(i => i.Addr))
                .ToArray();
        }

        public virtual async Task<ReadResult> ReadAsync(Command requestAddress, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            Logger.LogInfo(null, $"Выполнение запроса чтения по адресу {requestAddress} для устройства \"{Name}\"...");
            var request = SalachovRequest.CreateReadRequest(Id, requestAddress, scope);
            ReadResult result = null;
            var rawResult = await _pipe.RequestAsync(request, scope, cancellation);
            if (rawResult.Status == RequestStatus.OK)
            {
                var entities = CommonUtils.TryOrDefault
                    (() => getDataEntities(requestAddress, rawResult.To<SalachovResponse>(), _dataPacketDescriptors).ToArray(), null, out Exception ex);
                if (entities == null)
                {
                    Logger.LogError($"Не удалось разобрать данные в ответе", $"Произошла ошибка при десериализации полей ответа", ex);
                    result = new ReadResult(
                        ReadStatus.DESERIALIZATION_FAILURE,
                        null,
                        rawResult.Data,
                        rawResult.DataSections);
                }
                else
                {
                    result = new ReadResult(
                        mapRStatusToRRStatus(rawResult.Status),
                        entities,
                        rawResult.Data,
                        rawResult.DataSections);
                    if (requestAddress == Command.DATA_PACKET_CONFIGURATION_FILE)
                    {
                        _dataPacketDescriptors = EntitiesDeserializer.ExtractDataPacketDescriptors(result.Entities);
                    }
                }
            }
            else
            {
                Logger.LogError($"Не удалось выполнить запрос. Код ошибки: {rawResult.Status}", $"Произошла ошибка {rawResult.Status}. {rawResult.Status.GetEnumValueDescription()}");
                result = new ReadResult(
                    mapRStatusToRRStatus(rawResult.Status),
                    null,
                    rawResult.Data,
                    rawResult.DataSections);
            }

            return result;
        }
        protected ReadStatus mapRStatusToRRStatus(RequestStatus sendResults)
        {
            return map<ReadStatus>(sendResults);
        }

        public async Task<BurnResult> BurnAsync
            (Command requestAddress, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return await burnAsync(requestAddress, entities, false, scope, cancellation);
        }
        protected virtual async Task<BurnResult> burnAsync(Command requestAddress, IEnumerable<IDataEntity> entities, bool broadcast, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            Logger.LogInfo(null, $"Выполнение {broadcast.Ternar("широковещательного ", " ")}запроса записи по адресу {requestAddress} для устройства \"{Name}\"...");

            if (!await validateBurnOperationBySerial())
            {
                return new BurnResult(BurnStatus.FORBIDDEN_SERIAL, null);
            }
            var targetId = broadcast ? RUSDeviceId.ALL : Id;
            var request = SalachovRequest.CreateWriteRequest(targetId, requestAddress, entities, scope);
            var result = await _pipe.RequestAsync(request, scope, cancellation);
            var status = map<BurnStatus>(result.Status);
            if (result.Status == RequestStatus.OK)
            {
                var requestInfo = requestAddress.GetInfo();
                if (requestInfo.IsFileRequest) // If file burn request
                {
                    await Task.Delay(DELAY_BEFORE_FILE_BURN_VERIFICATION, cancellation);
                    var isVerified = await verifyBurnAsync();
                    if (!isVerified)
                    {
                        status = BurnStatus.VERIFICATION_ERROR;
                    }
                }
            }
            else
            {
                Logger.LogErrorEverywhere($"Не удалось выполнить запись. Код ошибки: {result.Status}");
            }

            return new BurnResult(status, result.Data, result.DataSections);

            async Task<bool> validateBurnOperationBySerial()
            {
                if (requestAddress.IsFileRequest())
                {
                    var fileSerial = Files.GetFileEntity(entities, FileEntityType.SERIAL_NUMBER).Value;
                    if (Equals(fileSerial, Files.DEFAULT_SERIAL))
                    {
                        Logger.LogError("Серийный номер не установлен", "Серийный номер не установлен");

                        return false;
                    }

                    var readResult = await ReadAsync(requestAddress, scope, cancellation);
                    if (readResult.Status == ReadStatus.OK)
                    {
                        var deviceSerial = Files.GetFileEntity(readResult.Entities, FileEntityType.SERIAL_NUMBER).Value;

                        if (!Equals(fileSerial, deviceSerial)
                            && !Equals(deviceSerial, Files.DEFAULT_SERIAL))
                        {
                            Logger.LogError("Серийные номера не совпадают", $"Серийный номер файла устройства не совпадает с серийным номером записываемого файла, {deviceSerial} != {fileSerial}");

                            return false;
                        }
                    }
                }

                return true;
            }

            async Task<bool> verifyBurnAsync()
            {
                var isVerified = false;
                using (Logger.Indent)
                {
                    Logger.LogInfo(null, $"Верификация записи...");

                    var actual = (await ReadAsync(requestAddress, scope, cancellation));
                    if (actual.Status == ReadStatus.OK)
                    {
                        var actualEntities = actual.Entities;
                        var expectedEntities = entities;
                        if (requestAddress.GetFileType() == FileType.DATA_PACKET_CONFIGURATION)
                        {
                            actualEntities = actual.Entities.Where(isTemplateEntity);
                            expectedEntities = entities.Where(isTemplateEntity);

                            bool isTemplateEntity(IDataEntity entity)
                                => Files.BaseFileTemplate.Any(ed => ed.Name == entity.Descriptor.Name);
                        }
                        var expectedData = expectedEntities.Select(e => e.RawValue).Flatten().ToArray();
                        var actualData = actualEntities.Select(e => e.RawValue).Flatten().ToArray();
                        isVerified = actualData.SequenceEqual(expectedData);
                        if (isVerified)
                        {
                            Logger.LogOKEverywhere($"Файл \"{requestAddress.GetFileType().GetInfo().FileName}\" записан", $"Верификация записи успешно завершена");
                        }
                        else
                        {
                            Logger.LogWarningEverywhere($"Считанный файл не совпадает с записанным");
                        }
                    }
                    else
                    {
                        Logger.LogWarningEverywhere($"Не удалось выполнить верификацию записи. Код ошибки: {actual.Status}");
                    }
                }

                return isVerified;
            }
        }

        T map<T>(RequestStatus sendResults)
            where T : struct, Enum
        {
            return EnumUtils.GetValues<T>()
                .Where(v => v.HasAttribute<RequestStatusMappingAttribute>())
                .ToDictionary(v => v.GetAttribute<RequestStatusMappingAttribute>())
                .Single(kvp => kvp.Key.RequestStatuses.Contains(sendResults))
                .Value;
        }

        /// <summary>
        /// Throws exception when the data are damaged
        /// </summary>
        /// <param name="address"></param>
        /// <param name="rawAnswer"></param>
        /// <returns></returns>
        IEnumerable<IDataEntity> getDataEntities(Command address, SalachovResponse rawAnswer, IEnumerable<EntityDescriptor> dataPacketDescriptors)
        {
            if (address == Command.DATA)
            {
                var descriptors = dataPacketDescriptors ?? getDescriptors();
                return EntitiesDeserializer.Deserialize(rawAnswer.Body, descriptors);

                IEnumerable<EntityDescriptor> getDescriptors()
                {
                    foreach (var i in rawAnswer.Body.Count().Range())
                    {
                        var format = DataEntityFormat.UINT8;
                        yield return new EntityDescriptor($"Byte{i}", i, 1, format, format.GetDefaultValidator());
                    }
                }
            }
            else if (address == Command.STATUS)
            {
                return EntitiesDeserializer.Deserialize(rawAnswer.Body, Requests.GetStatusRequestDescriptors(Id));
            }
            else if (address.IsFileRequest())
            {
                var version = FileDescriptorsTarget.ExtractFileVersion(rawAnswer.Body);
                var target = new FileDescriptorsTarget(address.GetFileType(), version, rawAnswer.Request.DeviceId);
                var descriptors = Files.Descriptors.Find(kvp => kvp.Key.Equals(target));
                if (descriptors.Found)
                {
                    return EntitiesDeserializer.Deserialize(rawAnswer.Body, descriptors.Value.Value.Descriptors);
                }
                else
                {
                    Logger.LogError(null, $"Не было найдено подходящих дескрипторов полей. Файл с версией \"{version}\" не поддерживается.");
                    throw new NotSupportedException();
                }
            }
            else
            {
                var description = Requests.GetRequestDescription(Id, rawAnswer.Request.Address);

                return EntitiesDeserializer.Deserialize(rawAnswer.Body, description.Descriptors);
            }
        }

        public async Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            await ThreadingUtils.ContinueAtThreadPull(cancellation);

            using (Logger.Indent)
            {
                Logger.LogInfo(null, $"Чтение статуса прибора \"{Name}\"...");

                var result = await ReadAsync(Command.STATUS, scope, cancellation);
                if (result.Status == ReadStatus.OK)
                {
                    var flagsEntity = result.Entities.ElementAt(0);
                    var numOfStatusBits = flagsEntity.Descriptor.Length.Length * 8;
                    var flags = (uint)(dynamic)flagsEntity.Value;
                    var serial = (ushort)result.Entities.ElementAt(1).Value;
                    var status = new DeviceStatusInfo.StatusInfo(numOfStatusBits, flags);

                    Logger.LogInfo(null, $"Статус: {status.ToBinString()}");

                    return new StatusReadResult(result, new DeviceStatusInfo(Id, serial, status));
                }
                else
                {
                    return new StatusReadResult(result, null);
                }
            }
        }

        public async Task ActivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            Logger.LogInfo(null, $"Активация устройства \"{Name}\"...");

            using (Logger.Indent)
            {
                await activateDeviceAsync(scope, cancellation);

                Logger.LogOK(null, $"Устройство \"{Name}\" активировано");
            }
        }
        protected virtual Task activateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation) => Task.CompletedTask;

        public async Task DeactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            Logger.LogInfo(null, $"Деактивация устройства \"{Name}\"...");

            using (Logger.Indent)
            {
                await deactivateDeviceAsync(scope, cancellation);

                Logger.LogOK(null, $"Устройство \"{Name}\" деактивировано");
            }
        }
        protected virtual Task deactivateDeviceAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation) => Task.CompletedTask;

        public T TryGetFeature<T>() where T : class, IRUSDeviceFeature
        {
            return null;
        }
    }
}