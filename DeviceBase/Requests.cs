using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;
using DeviceBase.Devices;
using DeviceBase.Models;
using Vectors;

namespace DeviceBase
{
    public static class Requests
    {
        internal static IEnumerable<EntityDescriptor> GetStatusRequestDescriptors(RUSDeviceId deviceId)
        {
            if (deviceId.IsOneOf(RUSDeviceId.TELEMETRY, RUSDeviceId.RUS_MODULE, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE))
            {
                yield return new EntityDescriptor("Flags", 0, 4, DataEntityFormat.UINT32);
                yield return new EntityDescriptor("Serial", 4, 2, DataEntityFormat.UINT16);
            }
            else
            {
                yield return new EntityDescriptor("Flags", 0, 2, DataEntityFormat.UINT16);
                yield return new EntityDescriptor("Serial", 2, 2, DataEntityFormat.UINT16);
            }
        }

        public static RequestInfo GetRequestDescription(RUSDeviceId deviceId, Command request)
        {
            var info = (deviceId, request);
            if (info.request.IsOneOf(Command.KEEP_MTF, Command.ROTATE_WITH_CONSTANT_SPEED, Command.DRILL_DIRECTLY, Command.TURN_ON_AZIMUTH))
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Inc", i, i.Add(2).DValue, DataEntityFormat.INT16),
                    new EntityDescriptor("Azi", i, i.Add(2).DValue, DataEntityFormat.INT16),
                    new EntityDescriptor("MTF", i, i.Add(2).DValue, DataEntityFormat.INT16),
                });
            }
            else if (info == (RUSDeviceId.LWD_LINK, Command.PBP_SETTINGS))
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Количество секторов", 0, 1, DataEntityFormat.UINT8),
                    new EntityDescriptor("Время молчания", 1, 1, DataEntityFormat.UINT8)
                });
            }
            else if (info == (RUSDeviceId.RUS_MODULE, Command.PWM_SET))
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Включить", 0, 2, DataEntityFormat.BOOLEAN),
                    new EntityDescriptor("Скважность", 2, 2, DataEntityFormat.UINT16, v => (dynamic)v >= 0 && (dynamic)v <= 100)
                });
            }
            else if (info == (RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, Command.FRAME_ACCUMULATION_TIME))
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Слово 1", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 2", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 3", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 4", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16)
                });
            }
            else if (info.request == Command.RETRANSLATE_PACKET)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Ретранслируемое слово", 0, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.BYTE_ARRAY)
                });
            }
            else if (info == (RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, Command.DEVICE_CONTROL_REGISTERS))
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Слово 1", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 2", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 3", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16),
                    new EntityDescriptor("Слово 4", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16)
                });
            }
            else if (info.request == Command.FLASH_ERASE)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[0]);
            }
            else if (info.request == Command.SET_DATA_UNLOAD_MODE)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[0]);
            }
            else if (info.request == Command.SET_FLASH_WORK_MODE)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[0]);
            }
            else if (info.request == Command.DOWNLOAD_FLASH)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[0]);
            }
            else if (info == (RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, Command.CALIBRATION_MODE_SET))
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("", 0, 2, DataEntityFormat.UINT16, x => true)
                });
            }
            else if (info.request == Command.PING)
            {
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    
                });
            }
            else if (info.request == Command.SET_TEST_MODE)
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Номер частоты", i.Value, i.Add(1).DValue, DataEntityFormat.UINT8, v => ((byte)v).IsOneOf((byte)0x01, (byte)0x02, (byte)0x03)),
                    new EntityDescriptor("Номер зонда", i.Value, i.Add(1).DValue, DataEntityFormat.UINT8, v => new IntInterval(1, 10).Contains((byte)v)),
                    new EntityDescriptor("Период генерации", i.Value, i.Add(1).DValue, DataEntityFormat.UINT8, v => new IntInterval(1, 100).Contains((byte)v)),
                });
            }
            else if (info == (RUSDeviceId.RUS_MODULE, Command.REG_DATA_FRAME))
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("CommandVersion", i.Add(2).PreviousValue, i.DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("PacketIndex", i.Add(4).PreviousValue, i.DValue, DataEntityFormat.UINT32, x => true),
                    new EntityDescriptor("TotalDurationInMs", i.Add(2).PreviousValue, i.DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("NumOfPoints", i.Add(2).PreviousValue, i.DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Points", i, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.UINT16_ARRAY, x => true),
                });
            }
            else if (info.request == Command.CLOCKS_SYNC)
            {
                var i = new SmartInt();
                return new RequestInfo(deviceId, request, new EntityDescriptor[]
                {
                    new EntityDescriptor("Время записи данных", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код года", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код месяца", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код дня", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код часа", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код минут", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код секунд", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Код миллисекунд", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Резерв 1", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Резерв 2", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                    new EntityDescriptor("Резерв 3", i.Value, i.Add(2).DValue, DataEntityFormat.UINT16, x => true),
                }, new int[] { 0 },
                ri =>
                {
                    var t = DateTime.UtcNow;
                    var values = new[] {
                        t.Year,
                        t.Month,
                        t.Day,
                        t.Hour,
                        t.Minute,
                        t.Second,
                        t.Millisecond
                    }.Select(v => v.ToUInt16()).ToArray();

                    return values.Length
                        .Range()
                        .Select(k => new DataEntity(values[k], ri.Descriptors[k + 1]));
                });
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static IEnumerable<IDataEntity> BuildInclinometrModeSetPacket(InclinometrMode mode)
        {
            var descriptor = new EntityDescriptor("", 0, 2, DataEntityFormat.UINT16, x => true);
            yield return new DataEntity((ushort)mode, descriptor);
        }

        public static IEnumerable<IDataEntity> BuildRegDataFramePacket(int packetIndex, int totalDurationInMs, ushort[] points)
        {
            var descriptors = GetRequestDescription(RUSDeviceId.RUS_MODULE, Command.REG_DATA_FRAME).Descriptors;
            yield return new DataEntity((ushort)0, descriptors[0]);
            yield return new DataEntity((uint)packetIndex, descriptors[1]);
            yield return new DataEntity((ushort)totalDurationInMs, descriptors[2]);
            yield return new DataEntity((ushort)points.Length, descriptors[3]);
            yield return new DataEntity((ushort[])points, descriptors[4]);
        }
    }
}
