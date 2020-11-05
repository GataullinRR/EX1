using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;

namespace DeviceBase.IOModels.EntitySerializers
{
    abstract class EntitySerializerBase : IEntitySerializer
    {
        public abstract DataEntityFormat[] SupportedFormats { get; }

        public object Deserialize(DataEntityFormat format, IEnumerable<byte> serialized)
        {
            throwIfFormatNotSupported(format);

            return deserialize(format, serialized);
        }

        public object DeserializeFromString(DataEntityFormat format, string serialized)
        {
            throwIfFormatNotSupported(format);

            return deserializeFromString(format, serialized);
        }

        public IEnumerable<byte> Serialize(DataEntityFormat format, object entity)
        {
            throwIfFormatNotSupported(format);

            return serialize(format, entity);
        }

        public string SerializeToString(DataEntityFormat format, object entity)
        {
            throwIfFormatNotSupported(format);

            return serializeToString(format, entity);
        }

        void throwIfFormatNotSupported(DataEntityFormat format)
        {
            if (SupportedFormats.NotContains(format))
            {
                throw new ArgumentException($"Формат {format} не поддерживается данным сериализатором");
            }
        }

        protected abstract object deserialize(DataEntityFormat format, IEnumerable<byte> serialized);
        protected abstract object deserializeFromString(DataEntityFormat format, string serialized);
        protected abstract IEnumerable<byte> serialize(DataEntityFormat format, object entity);
        protected abstract string serializeToString(DataEntityFormat format, object entity);

        protected IEnumerable<byte> serializeByAnotherSerializer(DataEntityFormat format, object entity)
        {
            return EntitySerializersFactory.GetSerializer(format).Serialize(format, entity);
        }
        protected string serializeToStringByAnotherSerializer(DataEntityFormat format, object entity)
        {
            return EntitySerializersFactory.GetSerializer(format).SerializeToString(format, entity);
        }
        protected object deserializeFromStringByAnotherSerializer(DataEntityFormat format, string entity)
        {
            return EntitySerializersFactory.GetSerializer(format).DeserializeFromString(format, entity);
        }

        protected IEnumerable<byte> takeInCorrectOrder(IEnumerable<byte> bytesSequence)
        {
            return bytesSequence.Reverse();
        }
    }
}
