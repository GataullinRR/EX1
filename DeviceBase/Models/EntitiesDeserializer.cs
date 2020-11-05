using Common;
using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;

namespace DeviceBase.Devices
{
    public static class EntitiesDeserializer
    {
        public static IEnumerable<EntityDescriptor> ExtractDataPacketDescriptors(IEnumerable<IDataEntity> dataEntities)
        {
            return dataEntities
                .Select(d => d.Value as DataPacketEntityDescriptor[])
                .SkipNulls()
                .Single()
                .Select(d => d.GetDescriptor());
        }

        internal static IEnumerable<IDataEntity> Deserialize
            (IEnumerable<byte> entitiesBytes, IEnumerable<EntityDescriptor> entitiesDescriptors)
        {
            var data = entitiesBytes.ToArray();
            EntityDescriptor lastDeserializingED = null;
            try
            {
                return deserializeDataFunc().ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Не удалось разобрать поле \"{lastDeserializingED?.Name}\"", $"Не удалось десериализовать поле. Имя: \"{lastDeserializingED?.Name}\" Позиция: {lastDeserializingED?.Position} Длина: {lastDeserializingED?.Length}", ex);

                throw;
            }

            IEnumerable<IDataEntity> deserializeDataFunc()
            {
                foreach (var descriptor in entitiesDescriptors)
                {
                    lastDeserializingED = descriptor;
                    // Exception if there are not enough bytes
                    var eData = descriptor.Length.IsTillTheEndOfAPacket
                        ? data.Skip(descriptor.Position).ToArray()
                        : data.GetRange(descriptor.Position, descriptor.Length.Length).ToArray();
                    yield return descriptor.InstantiateEntity(eData);

                    if (descriptor.Length.IsTillTheEndOfAPacket)
                    {
                        break;
                    }
                }
            }
        }
    }
}