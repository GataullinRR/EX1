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
    class DataPacketEntitiesDescriptorsArraySerializer : EntitySerializerBase
    {
        public override DataEntityFormat[] SupportedFormats { get; } = new DataEntityFormat[]
        {
             DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY
        };

        protected override object deserialize(DataEntityFormat format, IEnumerable<byte> serialized)
        {
            switch (format)
            {
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                    {
                        return serialized
                            .GroupBy(8)
                            .Select(ed => ed.StartEnumeration())
                            .Select(tryDeserialize).ToArray();

                        DataPacketEntityDescriptor tryDeserialize(Enumerator<byte> eDataEntityEnumerator)
                        {
                            var mnemonic = deserializeToASCIIString(eDataEntityEnumerator.Pull(4).ToArray());
                            var position = BitConverter.ToUInt16(takeInCorrectOrder(eDataEntityEnumerator.Pull(2)).ToArray(), 0);
                            var numOfBytes = eDataEntityEnumerator.AdvanceOrThrow();
                            eDataEntityEnumerator.AdvanceOrThrow();
                            var isSigned = (eDataEntityEnumerator.Current & 0b10000000) > 0;
                            var numOfSigBits = eDataEntityEnumerator.Current & (~0b10000000);

                            return new DataPacketEntityDescriptor(mnemonic, numOfBytes, position, numOfSigBits, isSigned);
                        }
                    }
                    
                default:
                    throw new NotSupportedEnumStateException(typeof(DataEntityFormat), format);
            }

            string deserializeToASCIIString(IEnumerable<byte> bytesSequence)
            {
                var arr = bytesSequence.ToArray();
                return CommonUtils.TryOrDefault(() => Encoding.ASCII.GetString(arr), "?".Repeat(arr.Length));
            }
        }

        protected override object deserializeFromString(DataEntityFormat format, string serialized)
        {
            switch (format)
            {
#warning code repetition! Incapsulate to another class!
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                    {
                        var entities = serialized.Split(Global.NL);
                        return parseEntities().ToArray();

                        IEnumerable<DataPacketEntityDescriptor> parseEntities()
                        {
                            foreach (var entity in entities)
                            {
                                var next = entity.AsEnumerable();

                                var name = takeString();
                                var position = takeNumber().ParseToUInt16Invariant();
                                var length = takeNumber().ParseToUInt8Invariant();
                                var numOfSignificantBits = takeNumber().ParseToUInt8Invariant();
                                var isSigned = takeName().ParseToBoolean();

                                yield return new DataPacketEntityDescriptor(
                                    name, 
                                    length, 
                                    position,
                                    numOfSignificantBits, 
                                    isSigned);

                                string takeString()
                                {
                                    var @string = entity.Between("\"", "\"", false, false);
                                    next = next.Skip(next.Find(@string).Index + @string.Length);

                                    return @string;
                                }
                                string takeNumber()
                                {
                                    var @string = next
                                        .SkipWhile(c => !char.IsDigit(c))
                                        .TakeWhile(char.IsDigit)
                                        .Aggregate();
                                    next = next.Skip(next.Find(@string).Index + @string.Length);

                                    return @string;
                                }
                                string takeName()
                                {
                                    var @string = next
                                        .SkipWhile(c => c != '=')
                                        .Skip(1)
                                        .SkipWhile(c => !char.IsLetter(c))
                                        .TakeWhile(char.IsLetter)
                                        .Aggregate();
                                    next = next.Skip(next.Find(@string).Index + @string.Length);

                                    return @string;
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
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                    {
                        var descriptors = (DataPacketEntityDescriptor[])entity;
                        return descriptors.Select(serialize).Flatten();

                        IEnumerable<byte> serialize(DataPacketEntityDescriptor descriptor)
                        {
                            var type = (byte)((descriptor.IsSigned ? (1 << 7) : 0) | descriptor.NumOfSignificantBits);
                            return new Enumerable<byte>
                            {
                                serializeByAnotherSerializer(DataEntityFormat.ASCII_STRING, descriptor.Name),
                                serializeByAnotherSerializer(DataEntityFormat.UINT16, (ushort)descriptor.Position),
                                serializeByAnotherSerializer(DataEntityFormat.UINT8, (byte)descriptor.Length),
                                serializeByAnotherSerializer(DataEntityFormat.UINT8, type),
                            };
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
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                    {
                        var entities = (DataPacketEntityDescriptor[])entity;
                        var sb = new StringBuilder();
                        foreach (var arrayEntity in entities)
                        {
                            sb.Append($"Name=\"{arrayEntity.Name}\" ");
                            sb.Append($"Position={arrayEntity.Position} ");
                            sb.Append($"Length={arrayEntity.Length} ");
                            sb.Append($"NumOfSignificantBits={arrayEntity.NumOfSignificantBits} ");
                            sb.Append($"IsSigned={arrayEntity.IsSigned} ");
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
