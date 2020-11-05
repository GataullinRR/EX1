using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Helpers;

namespace DeviceBase.IOModels.EntitySerializers
{
    class CalibrationFileEntitiesArraySerializer : EntitySerializerBase
    {
        static readonly Encoding CP1251_ENCODING = Encoding.GetEncoding("windows-1251");

        public override DataEntityFormat[] SupportedFormats { get; } = new DataEntityFormat[]
        {
             DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY
        };

        protected override object deserialize(DataEntityFormat format, IEnumerable<byte> serialized)
        {
            switch (format)
            {
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    {
                        var entities = new List<CalibrationFileEntity>();
                        var data = serialized.StartEnumeration();
                        try
                        {
                            while (true)
                            {
                                entities.Add(deserializeOne());
                            }
                        }
                        catch (InvalidOperationException) { }

                        return entities.ToArray();

                        CalibrationFileEntity deserializeOne()
                        {
                            var name = deserializeToCP1251String(data.AdvanceRangeOrThrow().TakeWhile(b => b != 0));
                            var itemType = (ItemTypes)data.AdvanceOrThrow();
                            var dataType = (DataTypes)data.AdvanceOrThrow();
                            var length = (ushort)EntitySerializersFactory
                                .GetSerializer(DataEntityFormat.UINT16)
                                .Deserialize(DataEntityFormat.UINT16, data.Pull(2));
                            var descriptorsData = getData();

                            return new CalibrationFileEntity(name, itemType, dataType, descriptorsData);

                            object getData()
                            {
                                if (dataType == DataTypes.STRING_CP1251)
                                {
                                    return CP1251_ENCODING.GetString(data.Pull(length));
                                }
                                else
                                {
                                    var size = dataType.GetSize();
                                    var count = length / size;
                                    var dataEntities = new object[count];
                                    for (int i = 0; i < count; i++)
                                    {
                                        switch (dataType)
                                        {
                                            case DataTypes.INT8:
                                                dataEntities[i] = (sbyte)EntitySerializersFactory
                                                    .GetSerializer(DataEntityFormat.INT8)
                                                    .Deserialize(DataEntityFormat.INT8, data.Pull(size));
                                                break;
                                            case DataTypes.INT16:
                                                dataEntities[i] = (short)EntitySerializersFactory
                                                    .GetSerializer(DataEntityFormat.INT16)
                                                    .Deserialize(DataEntityFormat.INT16, data.Pull(size));
                                                break;
                                            case DataTypes.UINT16:
                                                dataEntities[i] = (ushort)EntitySerializersFactory
                                                    .GetSerializer(DataEntityFormat.UINT16)
                                                    .Deserialize(DataEntityFormat.UINT16, data.Pull(size));
                                                break;
                                            case DataTypes.FLOAT:
                                                var raw = (uint)EntitySerializersFactory
                                                    .GetSerializer(DataEntityFormat.UINT32)
                                                    .Deserialize(DataEntityFormat.UINT32, data.Pull(size));
                                                dataEntities[i] = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
                                                break;

                                            default:
                                                throw new NotSupportedException();
                                        }
                                    }

                                    return dataEntities;
                                }
                            }
                        }
                    }

                default:
                    throw new NotSupportedEnumStateException(typeof(DataEntityFormat), format);
            }

            string deserializeToCP1251String(IEnumerable<byte> bytesSequence)
            {
                var arr = bytesSequence.ToArray();
                return CommonUtils.TryOrDefault(() => CP1251_ENCODING.GetString(arr), "?".Repeat(arr.Length));
            }
        }

        protected override object deserializeFromString(DataEntityFormat format, string serialized)
        {
            switch (format)
            {
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    {
                        var entities = serialized.Split(Global.NL);
                        return parseEntities().ToArray();

                        IEnumerable<CalibrationFileEntity> parseEntities()
                        {
                            foreach (var entity in entities)
                            {
                                var nameString = entity.Between("\"", "\"", false, false);
                                var next = entity.Skip(entity.Find(nameString).Index + nameString.Length);
                                var itemTypeString = next
                                    .SkipWhile(c => !char.IsDigit(c))
                                    .TakeWhile(char.IsDigit).Aggregate();
                                next = next.Skip(next.Find(itemTypeString).Index + itemTypeString.Length);
                                var dataTypeString = next
                                    .SkipWhile(c => !char.IsDigit(c))
                                    .TakeWhile(char.IsDigit).Aggregate();
                                next = next.Skip(next.Find(dataTypeString).Index + dataTypeString.Length);
                                var dataString = next
                                    .SkipWhile(c => c != '=')
                                    .Skip(1)
                                    .SkipWhile(c => c == '"')
                                    .TakeWhile(c => c != '"')
                                    .Aggregate();

                                var name = nameString;
                                var itemType = EnumUtils.CastSafe<ItemTypes>(itemTypeString.ParseToInt8Invariant());
                                var dataType = EnumUtils.CastSafe<DataTypes>(dataTypeString.ParseToInt8Invariant());
                                var data = parseData();

                                yield return new CalibrationFileEntity(name, itemType, dataType, data);

                                object parseData()
                                {
                                    switch (dataType)
                                    {
                                        case DataTypes.FLOAT:
                                            return dataString.Split(" ").Select(v => v.ParseToSingleInvariant()).ToArray();
                                        case DataTypes.UINT16:
                                            return dataString.Split(" ").Select(v => v.ParseToUInt16Invariant()).ToArray();
                                        case DataTypes.INT16:
                                            return dataString.Split(" ").Select(v => v.ParseToInt16Invariant()).ToArray();
                                        case DataTypes.INT8:
                                            return dataString.Split(" ").Select(v => v.ParseToInt8Invariant()).ToArray();
                                        case DataTypes.STRING_CP1251:
                                            return dataString;

                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            }
                        }
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        protected override IEnumerable<byte> serialize(DataEntityFormat format, object entity)
        {
            switch (format)
            {
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    {
                        var descriptors = (CalibrationFileEntity[])entity;
                        return descriptors.Select(serialize).Flatten();

                        IEnumerable<byte> serialize(CalibrationFileEntity descriptor)
                        {
                            var descriptorData = new Enumerable<byte>
                            {
                                CP1251_ENCODING.GetBytes(descriptor.Name),
                                0,
                                (byte)descriptor.ItemType,
                                (byte)descriptor.DataType,
                                EntitySerializersFactory
                                    .GetSerializer(DataEntityFormat.UINT16)
                                    .Serialize(DataEntityFormat.UINT16, (ushort)descriptor.DataLength)
                            };

                            switch (descriptor.DataType)
                            {
                                case DataTypes.INT8:
                                    {
                                        var bytes = ((Array)descriptor.Data)
                                            .ToEnumerable()
                                            .Select(o => (byte)o)
                                            .ToArray();
                                        descriptorData.Add(bytes);
                                    }
                                    break;
                                case DataTypes.INT16:
                                    {
                                        var bytes = ((Array)descriptor.Data)
                                            .ToEnumerable()
                                            .Select(o => (short)o)
                                            .Select(v => EntitySerializersFactory.GetSerializer(DataEntityFormat.INT16).Serialize(DataEntityFormat.INT16, (short)v))
                                            .Flatten()
                                            .ToArray();
                                        descriptorData.Add(bytes);
                                    }
                                    break;
                                case DataTypes.UINT16:
                                    {
                                        var bytes = ((Array)descriptor.Data)
                                            .ToEnumerable()
                                            .Select(o => (ushort)o)
                                            .Select(v => EntitySerializersFactory.GetSerializer(DataEntityFormat.UINT16).Serialize(DataEntityFormat.UINT16, (ushort)v))
                                            .Flatten()
                                            .ToArray();
                                        descriptorData.Add(bytes);
                                    }
                                    break;
                                case DataTypes.FLOAT:
                                    {
                                        var bytes = ((Array)descriptor.Data)
                                            .ToEnumerable()
                                            .Select(o => (float)o)
                                            .Select(v => BitConverter.ToUInt32(BitConverter.GetBytes(v), 0))
                                            .Select(v => EntitySerializersFactory.GetSerializer(DataEntityFormat.UINT32).Serialize(DataEntityFormat.UINT32, (uint)v))
                                            .Flatten()
                                            .ToArray();
                                        descriptorData.Add(bytes);
                                    }
                                    break;
                                case DataTypes.STRING_CP1251:
                                    {
                                        var bytes = CP1251_ENCODING.GetBytes((string)descriptor.Data);
                                        descriptorData.Add(bytes);
                                    }
                                    break;

                                default:
                                    throw new NotSupportedException();
                            }

                            return descriptorData;
                        }
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        protected override string serializeToString(DataEntityFormat format, object entity)
        {
            switch (format)
            {
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    {
                        var entities = (CalibrationFileEntity[])entity;
                        var sb = new StringBuilder();
                        foreach (var arrayEntity in entities)
                        {
                            sb.Append($"Name=\"{arrayEntity.Name}\" ");
                            sb.Append($"ItemType={(byte)arrayEntity.ItemType} ");
                            sb.Append($"DataType={(byte)arrayEntity.DataType} ");
                            sb.Append("Data=");
                            switch (arrayEntity.DataType)
                            {
                                case DataTypes.FLOAT:
                                case DataTypes.UINT16:
                                case DataTypes.INT16:
                                case DataTypes.INT8:
                                    var data = ((Array)arrayEntity.Data)
                                        .ToEnumerable()
                                        .Cast<dynamic>()
                                        .Select(v => (string)NumericEx.ToStringInvariant(v))
                                        .Aggregate(" ");
                                    sb.Append(data);
                                    break;

                                case DataTypes.STRING_CP1251:
                                    sb.Append($"\"{arrayEntity.Data}\"");
                                    break;

                                default:
                                    throw new NotSupportedException();
                            }
                            sb.AppendLine();
                        }

                        return sb.ToString();
                    }

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
