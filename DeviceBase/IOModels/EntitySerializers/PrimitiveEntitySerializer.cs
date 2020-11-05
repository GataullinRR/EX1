using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.IOModels.EntitySerializers
{
    class PrimitiveEntitySerializer : EntitySerializerBase
    {
        const ushort BOOL_TRUE = 0x00FF;
        const ushort BOOL_FALSE = 0x0000;

        public override DataEntityFormat[] SupportedFormats { get; } = new DataEntityFormat[]
        {
             DataEntityFormat.BOOLEAN,
             DataEntityFormat.INT8,
             DataEntityFormat.INT16,
             DataEntityFormat.INT32,
             DataEntityFormat.INT64,
             DataEntityFormat.UINT8,
             DataEntityFormat.UINT16,
             DataEntityFormat.UINT32,
             DataEntityFormat.UINT64,
             DataEntityFormat.ASCII_STRING,
             DataEntityFormat.BYTE_ARRAY,
             DataEntityFormat.UINT16_ARRAY,
        };

        protected override object deserialize(DataEntityFormat format, IEnumerable<byte> serialized)
        {
            var fixedData = takeInCorrectOrder(serialized).ToArray();
            switch (format)
            {
                case DataEntityFormat.BOOLEAN:
                    return BitConverter.ToUInt16(fixedData, 0) == BOOL_TRUE;
                case DataEntityFormat.UINT8:
                    return fixedData.Single();
                case DataEntityFormat.UINT16:
                    return BitConverter.ToUInt16(fixedData, 0);
                case DataEntityFormat.UINT32:
                    return BitConverter.ToUInt32(fixedData, 0);
                case DataEntityFormat.UINT64:
                    return BitConverter.ToUInt64(fixedData, 0);
                case DataEntityFormat.INT8:
                    return (sbyte)fixedData.Single();
                case DataEntityFormat.INT16:
                    return BitConverter.ToInt16(fixedData, 0);
                case DataEntityFormat.INT32:
                    return BitConverter.ToInt32(fixedData, 0);
                case DataEntityFormat.INT64:
                    return BitConverter.ToInt64(fixedData, 0);

                case DataEntityFormat.ASCII_STRING:
                    return deserializeToASCIIString(serialized);
                case DataEntityFormat.BYTE_ARRAY:
                    return serialized.ToArray();
                case DataEntityFormat.UINT16_ARRAY:
                    return serialized.GroupBy(2)
                        .Select(g => deserialize(DataEntityFormat.UINT16,g))
                        .ToArray();

                default:
                    throw new NotSupportedException();
            }

            string deserializeToASCIIString(IEnumerable<byte> bytesSequence)
            {
                var arr = bytesSequence.ToArray();
                return CommonUtils.TryOrDefault(() => Encoding.ASCII.GetString(arr), "?".Repeat(arr.Length));
            }
        }

        protected override object deserializeFromString(DataEntityFormat format, string serialized)
        {
            //byte[] bytes;
            //var fixedString = serialized.Skip(char.IsWhiteSpace).Aggregate();
            //if (fixedString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            //{
            //    var bytesArray = fixedString.GroupBy(2).Select(g => g.Aggregate()).Aggregate(" ");
            //    bytes = DeserializeFromString(DataEntityFormat.BYTE_ARRAY, bytesArray).To<byte[]>();
            //}

            switch (format)
            {
                case DataEntityFormat.BOOLEAN:
                    return serialized.ParseToBoolean();
                case DataEntityFormat.UINT8:
                    return serialized.ParseToUInt8Invariant();
                case DataEntityFormat.UINT16:
                    return serialized.ParseToUInt16Invariant();
                case DataEntityFormat.UINT32:
                    return serialized.ParseToUInt32Invariant();
                case DataEntityFormat.UINT64:
                    return serialized.ParseToUInt64Invariant();
                case DataEntityFormat.INT8:
                    return serialized.ParseToInt8Invariant();
                case DataEntityFormat.INT16:
                    return serialized.ParseToInt16Invariant();
                case DataEntityFormat.INT32:
                    return serialized.ParseToInt32Invariant();
                case DataEntityFormat.INT64:
                    return serialized.ParseToInt64Invariant();

                case DataEntityFormat.ASCII_STRING:
                    return serialized;
                case DataEntityFormat.BYTE_ARRAY:
                    return serialized
                        .Split(" ")
                        .Select(sb => sb.ParseToUInt8FromHexInvariant())
                        .ToArray();
                case DataEntityFormat.UINT16_ARRAY:
                    return serialized
                        .Split(" ")
                        .Select(sb => sb.ParseToUInt16FromHexInvariant())
                        .ToArray();

                default:
                    throw new NotSupportedException();
            }
        }

        protected override IEnumerable<byte> serialize(DataEntityFormat format, object entity)
        {
            switch (format)
            {
                case DataEntityFormat.BOOLEAN:
                    var encoded = ((bool)entity) 
                        ? BOOL_TRUE 
                        : BOOL_FALSE;
                    return serialize(DataEntityFormat.UINT16, encoded);
                case DataEntityFormat.UINT8:
                    return ((byte)entity).ToSequence();
                case DataEntityFormat.UINT16:
                    return takeInCorrectOrder(BitConverter.GetBytes((ushort)entity));
                case DataEntityFormat.UINT32:
                    return takeInCorrectOrder(BitConverter.GetBytes((uint)entity));
                case DataEntityFormat.UINT64:
                    return takeInCorrectOrder(BitConverter.GetBytes((ulong)entity));
                case DataEntityFormat.INT8:
                    return ((byte)(sbyte)entity).ToSequence();
                case DataEntityFormat.INT16:
                    return takeInCorrectOrder(BitConverter.GetBytes((short)entity));
                case DataEntityFormat.INT32:
                    return takeInCorrectOrder(BitConverter.GetBytes((int)entity)); ;
                case DataEntityFormat.INT64:
                    return takeInCorrectOrder(BitConverter.GetBytes((long)entity));

                case DataEntityFormat.ASCII_STRING:
                    return Encoding.ASCII.GetBytes((string)entity);
                case DataEntityFormat.BYTE_ARRAY:
                    return (byte[])entity;
                case DataEntityFormat.UINT16_ARRAY:
                    return ((UInt16[])entity)
                        .Select(v => serialize(DataEntityFormat.UINT16, v))
                        .Flatten()
                        .ToArray();

                default:
                    throw new NotSupportedException();
            }
        }

        protected override string serializeToString(DataEntityFormat format, object entity)
        {
            switch (format)
            {
                case DataEntityFormat.BOOLEAN:
                    return ((bool)entity).ToString();
                case DataEntityFormat.UINT8:
                    return ((byte)entity).ToString();
                case DataEntityFormat.UINT16:
                    return ((ushort)entity).ToString();
                case DataEntityFormat.UINT32:
                    return ((uint)entity).ToString();
                case DataEntityFormat.UINT64:
                    return ((ulong)entity).ToString();
                case DataEntityFormat.INT8:
                    return ((sbyte)entity).ToString();
                case DataEntityFormat.INT16:
                    return ((short)entity).ToString();
                case DataEntityFormat.INT32:
                    return ((int)entity).ToString();
                case DataEntityFormat.INT64:
                    return ((long)entity).ToString();

                case DataEntityFormat.ASCII_STRING:
                    return (string)entity;
                case DataEntityFormat.BYTE_ARRAY:
                    return ((byte[])entity)
                        .Select(b => b.ToString("X2"))
                        .Aggregate(" ");
                case DataEntityFormat.UINT16_ARRAY:
                    return ((byte[])entity)
                        .Select(b => b.ToString("X4"))
                        .Aggregate(" ");

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
