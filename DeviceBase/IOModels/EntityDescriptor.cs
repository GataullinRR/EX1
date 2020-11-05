using DeviceBase.Attributes;
using DeviceBase.Helpers;
using DeviceBase.IOModels.EntitySerializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TinyConfig;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;

namespace DeviceBase.IOModels
{
    public enum DataEntityFormat
    {
        [DefaultEntityValue((bool)false)]
        BOOLEAN,
        [DefaultEntityValue((sbyte)0)]
        INT8,
        [DefaultEntityValue((short)0)]
        INT16,
        [DefaultEntityValue((int)0)]
        INT32,
        [DefaultEntityValue((long)0)]
        INT64,
        [DefaultEntityValue((byte)0)]
        UINT8,
        [DefaultEntityValue((ushort)0)]
        UINT16,
        [DefaultEntityValue((uint)0)]
        UINT32,
        [DefaultEntityValue((ulong)0)]
        UINT64,
        [DefaultEntityValue("")]
        ASCII_STRING,
        [DefaultEntityValue(new byte[0])]
        BYTE_ARRAY,
        [DefaultEntityValue(new ushort[0])]
        UINT16_ARRAY,
        /// <summary>
        /// <see cref="DataPacketEntityDescriptor"/>
        /// </summary>
        [DefaultEntityValue(typeof(DataPacketEntityDescriptor))]
        DATA_PACKET_ENTITIES_ARRAY,
        /// <summary>
        /// <see cref="CalibrationFileEntity"/>
        /// </summary>
        [DefaultEntityValue(typeof(CalibrationFileEntity))]
        CALIBRATION_PACKET_ENTITIES_ARRAY,
    }

    public class EntityDescriptor : IEquatable<EntityDescriptor>
    {
        static readonly DataEntityFormat[] POINT_DATA_ENTITIES = new[]
        {
            DataEntityFormat.INT8, DataEntityFormat.UINT8,
            DataEntityFormat.INT16, DataEntityFormat.UINT16,
            DataEntityFormat.INT32, DataEntityFormat.UINT32,
            DataEntityFormat.INT64, DataEntityFormat.UINT64
        };
        static readonly Encoding CP1251_ENCODING = Encoding.GetEncoding("windows-1251");

        public string Name { get; }
        /// <summary>
        /// Position in the packet in bytes counting from the beginning of data sequence. [0;Inf).
        /// </summary>
        public int Position { get; }
        /// <summary>
        /// The full length in bytes
        /// </summary>
        public EntityLength Length { get; }
        public DataEntityFormat ValueFormat { get; }
        public Func<object, bool> ValidateValueRange { get; }
        public IDataEntity DefaultEntity { get; }

        public EntityDescriptor(
            string name,
            int position,
            EntityLength length,
            DataEntityFormat valueFormat)
            :this(name, position, length, valueFormat, valueFormat.GetDefaultValidator())
        {
            
        }
        public EntityDescriptor(
            string name, 
            int position, 
            EntityLength length, 
            DataEntityFormat valueFormat, 
            Func<object, bool> valueRangeValidator)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Position = position;
            Length = length;
            ValueFormat = valueFormat;
            ValidateValueRange = valueRangeValidator ?? throw new ArgumentNullException(nameof(valueRangeValidator));

            var defaultValue = valueFormat.GetAttribute<DefaultEntityValueAttribute>().Value;
            DefaultEntity = new DataEntity(defaultValue, this);
        }

        public IDataEntity InstantiateEntity(IEnumerable<byte> entityData)
        {
            var value = Deserialize(entityData);
            var rawValue = new Lazy<IEnumerable<byte>>(() => Serialize(value));
            var isPoint = POINT_DATA_ENTITIES.Contains(ValueFormat);

            return isPoint
                 ? new PointDataEntity(value, rawValue, this)
                 : new DataEntity(value, rawValue, this);
        }

        /// <summary>
        /// To raw bytes
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="SerializationException"></exception>
        /// <exception cref="Exception"></exception>
        /// <returns></returns>
        public IEnumerable<byte> Serialize(object value)
        {
            return EntitySerializersFactory
                .GetSerializer(ValueFormat)
                .Serialize(ValueFormat, value);
        }

        public string SerializeToString(object value)
        {
            return EntitySerializersFactory
                .GetSerializer(ValueFormat)
                .SerializeToString(ValueFormat, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialized"></param>
        /// <exception cref="SerializationException"></exception>
        /// <exception cref="Exception"></exception>
        /// <returns></returns>
        public object Deserialize(IEnumerable<byte> serialized)
        {
            if (!Length.IsTillTheEndOfAPacket)
            {
                serialized = serialized.Take(Length.Length);
            }

            return EntitySerializersFactory
                .GetSerializer(ValueFormat)
                .Deserialize(ValueFormat, serialized);
        }

        /// <summary>
        /// May throw exception!
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public object DeserializeFromString(string serialized)
        {
            return EntitySerializersFactory
                .GetSerializer(ValueFormat)
                .DeserializeFromString(ValueFormat, serialized);
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityDescriptor);
        }
        public bool Equals(EntityDescriptor other)
        {
            return other != null &&
                   Name == other.Name &&
                   Position == other.Position &&
                   EqualityComparer<EntityLength>.Default.Equals(Length, other.Length) &&
                   ValueFormat == other.ValueFormat;
        }

        public override int GetHashCode()
        {
            var hashCode = 1143507782;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Position.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityLength>.Default.GetHashCode(Length);
            hashCode = hashCode * -1521134295 + ValueFormat.GetHashCode();
            return hashCode;
        }
    }
}
