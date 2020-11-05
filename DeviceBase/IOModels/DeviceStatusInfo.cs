using DeviceBase.Devices;
using DeviceBase.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
    public class DeviceStatusInfo
    {
        public class StatusInfo
        {
            public int NumOfBits { get; }
            public uint Bits { get; }

            public StatusInfo(int numOfBits, uint bits)
            {
                NumOfBits = numOfBits;
                Bits = bits;
            }

            public string ToBinString()
            {
                return Convert.ToString(Bits, 2).PadLeft(NumOfBits, '0');
            }
        }

        class FlagDescriptor
        {
            public FlagDescriptor(RUSDeviceId? deviceId, int bitIndex, string description)
            {
                DeviceId = deviceId;
                BitIndex = bitIndex;
                Description = description ?? throw new ArgumentNullException(nameof(description));
            }

            public RUSDeviceId? DeviceId { get; }
            public int BitIndex { get; }
            public string Description { get; }
        }

        static IReadOnlyList<FlagDescriptor> DESCRIPTORS { get; }
            = new List<FlagDescriptor>()
            {
                 new FlagDescriptor(null, 0, "Питание платы не в норме"),
                 new FlagDescriptor(null, 1, "Превышение температуры платы"),
                 new FlagDescriptor(null, 2, "Ошибка чтения калибровок"),
                 new FlagDescriptor(null, 3, "Ошибка чтения темп-ых калибровок"),
                 new FlagDescriptor(null, 4, "Режим температурной калибровки"),
                 new FlagDescriptor(null, 5, "Режим угловой калибровки"),
                 new FlagDescriptor(null, 7, "Питание прибора не в норме"),
                 new FlagDescriptor(null, 8, "Температура прибора не в норме"),
                 new FlagDescriptor(null, 9, "Питание прибора от резерва"),
                 new FlagDescriptor(null, 10, "Запись во флеш-память выключена"),
                 new FlagDescriptor(null, 11, "Признаки отказа флеш-памяти"),
                 new FlagDescriptor(null, 14, "Ошибка вычисления угла"),
                 new FlagDescriptor(null, 15, "Ошибка усреднения угла"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 16, "Ошибка инклинометра"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 17, "Ошибка измерителя давления"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 18, "Ошибка платы ударов"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 19, "Ошибка часов RTС"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 20, "Ошибка ГК"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 21, "Ошибка EEPROM"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 22, "Ошибка питания"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 23, "Температуре вне пределов"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 24, "Режим клибровки"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 25, "Ошибка батарейного модуля"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 26, "Превышение наработки"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 27, "Запись во флеш-память выключена"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 28, "Отказ флеш-памяти"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 29, "Мало пямяти (<10%)"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 30, "Ударная перегрузка"),
                 new FlagDescriptor(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 31, "Превышение давления"),
            }.AsReadOnly();

        /// <summary>
        /// <see cref="null"/> when provided from status menmonic of a DataRequest
        /// </summary>
        public ushort? SerialNumber { get; }
        public StatusInfo Status { get; }
        public IEnumerable<string> CurrentStatuses { get; }
        /// <summary>
        /// <see cref="null"/> if incorrect
        /// </summary>
        public InclinometrMode? InclinometrMode { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="serialNumber"><see cref="null"/> if not provided</param>
        /// <param name="status"></param>
        public DeviceStatusInfo(RUSDeviceId deviceId, ushort? serialNumber, StatusInfo status)
        {
            SerialNumber = serialNumber;
            Status = status;

            var deviceFlagsDescriptors = DESCRIPTORS
                .Where(s => s.DeviceId == deviceId || s.DeviceId == null)
                .OrderBy(d => d.DeviceId == null)
                .ToArray();
            CurrentStatuses = ArrayUtils
                .Range(0, 1, Status.NumOfBits)
                .Select(v => 2L.Pow(v))
                .Select(mask => (Status.Bits & mask) > 0)
                .Select((set, i) =>
                {
                    var descriptor = deviceFlagsDescriptors.Find(d => d.BitIndex == i);
                    return set 
                        ? descriptor.ValueOrDefault?.Description 
                        : null;
                })
                .SkipNulls();

            var modes = EnumUtils
                .GetValues<InclinometrMode>()
                .Where(m => m.GetInfo().IsThisStatus((ushort)Status.Bits))
                .ToArray();
            if (modes.Length == 1)
            {
                InclinometrMode = modes.Single();
            }
        }
    }
}
