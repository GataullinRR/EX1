using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.Runtime.Serialization;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using Common;
using DeviceBase.Devices;

namespace DeviceBase.Models
{
    public class FileStringSerializer
    {
        const string KVP_SEPARATOR = ":";

        public string Serialize(IEnumerable<IDataEntity> fileData)
        {
            var sb = new StringBuilder();
            foreach (var item in fileData)
            {
                var serializedValue = item.Descriptor.SerializeToString(item.Value);
                if (serializedValue.Contains(Global.NL))
                {
                    if (item.Descriptor.Length.IsTillTheEndOfAPacket &&
                        item.Descriptor.ValueFormat == DataEntityFormat.ASCII_STRING)
                    {
                        sb.AppendLine($"{item.Descriptor.Name}:{serializedValue}");
                    }
                    else
                    {
                        var name = item.Descriptor.Name + "[]";
                        var serializedValues = serializedValue.Split(Global.NL);
                        foreach (var value in serializedValues)
                        {
                            sb.AppendLine($"{name}:{value}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"{item.Descriptor.Name}:{serializedValue}");
                }
            }

            sb.Remove(sb.Length - Global.NL.Length, Global.NL.Length);
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="fileType"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        /// <exception cref="SerializationException"></exception>
        /// <exception cref="Exception"></exception>
        public IEnumerable<IDataEntity> Deserialize(string fileData, FileType fileType, RUSDeviceId deviceId)
        {
            var entries = parse().ToArray();
            validateFileTypePointer();
            var target = getTarget();
            var descriptors = Files.Descriptors.Find(kvp => kvp.Key.Equals(target));
            if (!descriptors.Found)
            {
                throw new SerializationException("There were no appropriate descriptors found");
            }
            else
            {
                foreach (var descriptor in descriptors.Value.Value.Descriptors)
                {
                    var entry = entries.Find(e => e.Name == descriptor.Name);
                    if (!entry.Found)
                    {
                        throw new SerializationException($"The file isn't full. It lacks \"{descriptor.Name}\" entity.");
                    }
                    else
                    {
#warning doesn't work for all types..., bad code
                        if (descriptor.Length.IsTillTheEndOfAPacket &&
                            descriptor.ValueFormat == DataEntityFormat.ASCII_STRING)
                        {
                            var startOfEntityIndex = fileData.Find(descriptor.Name).Index;
                            var startIndex = fileData
                                .Skip(startOfEntityIndex)
                                .Find(KVP_SEPARATOR)
                                .Index + startOfEntityIndex + KVP_SEPARATOR.Length;
                            var multilineValue = fileData.Skip(startIndex).Aggregate();
                            if (multilineValue.Length % 2 == 1)
                            {
                                multilineValue += (char)0;
                            }

                            var value = (string)descriptor.DeserializeFromString(multilineValue);
                            if (value != descriptor.SerializeToString(value))
                            {
                                throw new SerializationException();
                            }

                            yield return new DataEntity(value, descriptor.Serialize(value), descriptor);
                        }
                        else
                        {
                            var value = descriptor.DeserializeFromString(entry.Value.SerializedValue);
                            var rawValue = descriptor.Serialize(value).ToArray();

                            if (!descriptor.Length.IsTillTheEndOfAPacket
                                && rawValue.Length != descriptor.Length.Length)
                            {
                                throw new SerializationException($"Length for field \"{descriptor.Name}\" is not valid. Expected:{descriptor.Length.Length}, actual:{rawValue.Length}");
                            }

                            yield return new DataEntity(value, rawValue, descriptor);
                        }
                    }
                }
            }

            /////////////////////////////////////////////////////

            FileDescriptorsTarget getTarget()
            {
                var version = getEntityValue(FileEntityType.FORMAT_VERSION);

                return new FileDescriptorsTarget(fileType, version, deviceId);
            }

            void validateFileTypePointer()
            {
                var fileTypePtr = getEntityValue(FileEntityType.FILE_TYPE_POINTER);
                var expected = fileType.GetInfo().FileTypePointerName;
                if (fileTypePtr != expected)
                {
                    throw new SerializationException($"FileTypePointer is not valid. Expected \"{expected}\", but was \"{fileTypePtr}\"");
                }
            }

            string getEntityValue(FileEntityType entityType)
            {
                var entry = Files.BaseFileTemplate.Single(d => d.EntityType == entityType);
                var entityValue = entries.SingleOrDefault(e => e.Name == entry.Name).SerializedValue;
                if (entityValue == null)
                {
                    throw new SerializationException($"There was no \"{entityType}\" entity present.");
                }

                return entityValue;
            }

            IEnumerable<(string Name, string SerializedValue)> parse()
            {
                var parsedLines = fileData
                    .Split(Global.NL)
                    .Select(tryParseLine)
                    .SkipNulls()
                    .Select(v => v.Value)
                    .GroupBy(v => v.Name, v => v.SerializedValue);
                foreach (var entity in parsedLines)
                {
                    var isMultiline = entity.Key.EndsWith("[]");
                    var lines = entity.ToArray();
                    if (!isMultiline && lines.Length > 1)
                    {
                        throw new SerializationException($"Не допускается наличие повторяющихся полей. Имя поля: \"{entity.Key}\"");
                    }
                    else
                    {
                        var name = isMultiline
                            ? entity.Key.SkipFromEnd(2).Aggregate()
                            : entity.Key;
                        var value = lines
                            .Aggregate(Global.NL)
                            .Aggregate();

                        yield return (name, value);
                    }
                }

                (string Name, string SerializedValue)? tryParseLine(string line)
                {
                    var values = line.Split(KVP_SEPARATOR, false);
                    if (values.Length >= 2)
                    {
                        var name = values[0];
                        var value = values.Skip(1).Aggregate(KVP_SEPARATOR);

                        return (name, value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}
