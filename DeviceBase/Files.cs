using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Helpers;
using DeviceBase.Attributes;
using DeviceBase.IOModels;
using DeviceBase.Devices;

namespace DeviceBase
{
    using FED = FileEntityDescriptor;
    using FBED = FileBaseEntityDescriptor;

    public enum FileType
    {
        [FileTypeInfo("S_", "Заводские настройки", FACTORY_SETTINGS)]
        FACTORY_SETTINGS,
        [FileTypeInfo("F_", "Формат пакета", DATA_PACKET_CONFIGURATION)]
        DATA_PACKET_CONFIGURATION,
        [FileTypeInfo("F_", "Формат пакета во Flash", FLASH_DATA_PACKET_CONFIGURATION)]
        FLASH_DATA_PACKET_CONFIGURATION,
        [FileTypeInfo("K_", "Калибровки", CALIBRATION)]
        CALIBRATION,

        /// <summary>
        /// For <see cref="RUSInclinometr"/>
        /// </summary>
        [FileTypeInfo("T_", "Температурные калибровки", TEMPERATURE_CALIBRATION)]
        TEMPERATURE_CALIBRATION,

        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [FileTypeInfo("R_", "Режим работы", WORK_MODE)]
        WORK_MODE
    }

    public enum FileEntityType
    {
        FORMAT_VERSION,
        BURN_YEAR,
        BURN_MONTH,
        BURN_DAY,
        SERIAL_NUMBER,
        MODIFICATION,
        FILE_TYPE_POINTER,

        ANOTHER
    }

    public class FileBaseEntityDescriptor : EntityDescriptor
    {
        public FileEntityType EntityType { get; }

        public FileBaseEntityDescriptor
            (string name, int position, int length, DataEntityFormat valueFormat, FileEntityType entityType)
            : base(name, position, length, valueFormat, getValidator(valueFormat, entityType))
        {
            EntityType = entityType;
        }

        static Func<dynamic, bool> getValidator(DataEntityFormat valueFormat, FileEntityType entityType)
        {
            switch (entityType)
            {
                case FileEntityType.FORMAT_VERSION:
                    return x => ((string)x).All(char.IsLetterOrDigit);
                case FileEntityType.BURN_YEAR:
                    return x => ((string)x).All(char.IsDigit);
                case FileEntityType.BURN_MONTH:
                    return x => ((string)x).All(char.IsDigit) && x.ParseToIntInvariant() <= 12;
                case FileEntityType.BURN_DAY:
                    return x => ((string)x).All(char.IsDigit) && x.ParseToIntInvariant() <= 31;
                case FileEntityType.SERIAL_NUMBER:
                    return valueFormat.GetDefaultValidator();
                case FileEntityType.MODIFICATION:
                    return x => ((string)x).All(char.IsLetterOrDigit);
                case FileEntityType.ANOTHER:
                    return valueFormat.GetDefaultValidator();
                case FileEntityType.FILE_TYPE_POINTER:
                    return x => "TFSK".Contains(x[0]) && " _".Contains(x[1]);

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class FileEntityDescriptor : EntityDescriptor
    {
        public object DefaultValue { get; }
        public IDataEntity FileDefaultDataEntity => new DataEntity(DefaultValue, Serialize(DefaultValue), this);

        public FileEntityDescriptor(
            string name,
            int position,
            EntityLength length,
            DataEntityFormat valueFormat,
            object defaultValue)
        : this(name, position, length, valueFormat, valueFormat.GetDefaultValidator(), defaultValue)
        {
            DefaultValue = defaultValue;
        }
        public FileEntityDescriptor(
            string name, 
            int position, 
            EntityLength length,
            DataEntityFormat valueFormat,
            Func<object, bool> valueRangeValidator, 
            object defaultValue)
            : base(name, position, length, valueFormat, valueRangeValidator)
        {
            DefaultValue = defaultValue;
        }
    }

    public class FileDescriptorsTarget
    {
        public FileType FileType { get; }
        public string FileFormatVersion { get; }
        public RUSDeviceId TargetDeviceId { get; }

        public FileDescriptorsTarget(FileType fileType, string fileFormatVersion, RUSDeviceId targetDeviceId)
        {
            FileType = fileType;
            FileFormatVersion = fileFormatVersion;
            TargetDeviceId = targetDeviceId;
        }

        public override bool Equals(object obj)
        {
            if (obj is FileDescriptorsTarget other)
            {
                return FileType == other.FileType
                    && TargetDeviceId == other.TargetDeviceId
                    && FileFormatVersion == other.FileFormatVersion;
            }
            else
            {
                return false;
            }
        }

        public static string ExtractFileVersion(IEnumerable<byte> filePacketBody)
        {
            return Encoding.ASCII.GetString(filePacketBody.Skip(10).Take(2).ToArray());
        }

        public override int GetHashCode()
        {
            var hashCode = -2129268422;
            hashCode = hashCode * -1521134295 + FileType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileFormatVersion);
            hashCode = hashCode * -1521134295 + TargetDeviceId.GetHashCode();
            return hashCode;
        }
    }

    public class FileStructure
    {
        public FED[] Descriptors { get; }

        public FileStructure(FED[] descriptors)
        {
            Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        }
    }

    public static class Files
    {
        public const ushort DEFAULT_SERIAL = ushort.MaxValue;

        /// <summary>
        /// For all FileTypes, FileVersions, Devices
        /// </summary>
        public static IReadOnlyList<FBED> BaseFileTemplate { get; }
        /// <summary>
        /// It contains descriptions of the all entities.
        /// </summary>
        public static IReadOnlyDictionary<FileDescriptorsTarget, FileStructure> Descriptors { get; }

        static Files()
        {
            var pos = new SmartInt();
            var dpedPos = new SmartInt();
            RUSDeviceId id = 0;
            string name = null;

            var descriptors = new Dictionary<FileDescriptorsTarget, FileStructure>();

            BaseFileTemplate = new FBED[]
            {
                new FBED("Краткое название прибора", pos, pos.Add(8).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.ANOTHER),
                new FBED("Модификация", pos, pos.Add(2), DataEntityFormat.ASCII_STRING, FileEntityType.MODIFICATION),
                new FBED("Версия ПО или пакета данных", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.FORMAT_VERSION),
                new FBED("Дата составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.BURN_DAY),
                new FBED("Месяц составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.BURN_MONTH),
                new FBED("Год составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.BURN_YEAR),
                new FBED("Указатель типа файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, FileEntityType.FILE_TYPE_POINTER),
                new FBED("Серийный номер прибора", pos, pos.Add(2).DValue, DataEntityFormat.UINT16, FileEntityType.SERIAL_NUMBER),
            };

            #region Telemetry 0b00000001

            name = "________";
            id = RUSDeviceId.TELEMETRY;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region RUSModule 0b00000010

            name = "________";
            id = RUSDeviceId.RUS_MODULE;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region DriveControll 0b00000110

            name = "________";
            id = RUSDeviceId.DRIVE_CONTROL;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region Telesystem 0b00001000

            name = "________";
            id = RUSDeviceId.TELESYSTEM;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region RUSEMCModule 0b01010000

            name = "EMC_Mdl_";
            id = RUSDeviceId.EMC_MODULE;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region TechnologicalModule 0b00001000

            name = "TexModul";
            id = RUSDeviceId.RUS_TECHNOLOGICAL_MODULE;
            pos.Value = 0;

            {
                pos.Value = 0;
                var v01Target = new FileDescriptorsTarget(FileType.FACTORY_SETTINGS, "01", id);
                var v01 = new Enumerable<FED>
                {
                    createHeader(name, FileType.FACTORY_SETTINGS.GetInfo().FileTypePointerName),
                    new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                    new FED("Массив UInt8[36]", pos, pos.Add(36).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                }.ToArray();

                descriptors.Add(v01Target, new FileStructure(v01));
            }
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            {
                pos.Value = 0;
                var v01CalibrationTarget
                    = new FileDescriptorsTarget(FileType.CALIBRATION, "01", id);
                var v01Calibration = new Enumerable<FED>
                {
                    createHeader(name, FileType.CALIBRATION.GetInfo().FileTypePointerName),
                    new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                    new FED("Калибровки", pos, pos.Add(36).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                }.ToArray();

                descriptors.Add(v01CalibrationTarget, new FileStructure(v01Calibration));
            }

            {
                pos.Value = 0;
                var v01TemperatureCalibrationTarget
                    = new FileDescriptorsTarget(FileType.TEMPERATURE_CALIBRATION, "01", id);
                var v01TemperatureCalibration = new Enumerable<FED>
                {
                    createHeader(name, FileType.TEMPERATURE_CALIBRATION.GetInfo().FileTypePointerName),
                    new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                    new FED("Калибровки", pos, pos.Add(36).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue]),
                }.ToArray();

                descriptors.Add(v01TemperatureCalibrationTarget, new FileStructure(v01TemperatureCalibration));
            }

            pos.Value = 0;
            var v01CalibrationFileForTechnologicalModuleTarget
                = new FileDescriptorsTarget(FileType.WORK_MODE, "01", id);
            var v01CalibrationFileForTechnologicalModule = new Enumerable<FED>
            {
                createHeader(name, FileType.WORK_MODE.GetInfo().FileTypePointerName),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Режим работы", pos, pos.Add(36).DValue, DataEntityFormat.BYTE_ARRAY, new byte[pos.DValue])
            }.ToArray();

            descriptors.Add(v01CalibrationFileForTechnologicalModuleTarget, new FileStructure(v01CalibrationFileForTechnologicalModule));

            #endregion

            #region RUSLWDLink 0b00001001

            name = "SNAPUNIT";
            id = RUSDeviceId.LWD_LINK;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);
            registerStandard(FileType.CALIBRATION);
            registerStandard(FileType.DATA_PACKET_CONFIGURATION);
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            #endregion

            #region ShockSensor 0b00000101

            name = "SHOCK___";
            id = RUSDeviceId.SHOCK_SENSOR;

            registerStandard(FileType.FACTORY_SETTINGS);

            pos.Value = 0;
            var v01DataPacketConfigurationFileForShockSensorTarget
                = new FileDescriptorsTarget(FileType.DATA_PACKET_CONFIGURATION, "01", id);
            var v01DataPacketConfigurationFileForShockSensor = new Enumerable<FED>
            {
                createHeader(name, "F_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                    new DataPacketEntityDescriptor[]
                    {
                        new DataPacketEntityDescriptor("SH5O", 2, 1, false),
                        new DataPacketEntityDescriptor("SH1O", 2, 3, false),
                        new DataPacketEntityDescriptor("SH2O", 2, 5, false),
                        new DataPacketEntityDescriptor("SH3O", 2, 7, false),
                        new DataPacketEntityDescriptor("SH5R", 2, 9, false),
                        new DataPacketEntityDescriptor("SH1R", 2, 11, false),
                        new DataPacketEntityDescriptor("SH2R", 2, 13, false),
                        new DataPacketEntityDescriptor("SH3R", 2, 15, false),
                        new DataPacketEntityDescriptor("ARAD", 2, 17, false),
                        new DataPacketEntityDescriptor("AOSV", 2, 19, false),
                    }),
            }.ToArray();
            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            pos.Value = 0;
            var v01CalibrationFileForShockSensorTarget
                = new FileDescriptorsTarget(FileType.CALIBRATION, "01", id);
            var v01CalibrationFileForShockSensor = new Enumerable<FED>
            {
                createHeader(name, "K_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Калибровки", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY,
                new CalibrationFileEntity[]
                {
                    CalibrationFileEntity.CreateConstantsTable("G0_", DataTypes.UINT16, new ushort[] { 517, 517, 517 }),
                    CalibrationFileEntity.CreateConstantsTable("POR_", DataTypes.UINT16, new ushort[] { 41, 82, 164, 246 }),
                    CalibrationFileEntity.CreateConstantsTable("TMR", DataTypes.UINT16, new ushort[] { 2 }),
                }),
            }.ToArray();


            descriptors.Add(v01CalibrationFileForShockSensorTarget, new FileStructure(v01CalibrationFileForShockSensor));
            descriptors.Add(v01DataPacketConfigurationFileForShockSensorTarget, new FileStructure(v01DataPacketConfigurationFileForShockSensor));

            #endregion

            #region RotationSensor 0b00000111

            name = "Bl_Gyro_";
            id = RUSDeviceId.ROTATIONS_SENSOR;
            pos.Value = 0;
            dpedPos.Value = 1;

            registerStandard(FileType.FACTORY_SETTINGS);

            pos.Value = 0;
            var v01DataPacketConfigurationFileForRotationSensorTarget
                = new FileDescriptorsTarget(FileType.DATA_PACKET_CONFIGURATION, "01", id);
            var v01DataPacketConfigurationFileForRotationSensor = new Enumerable<FED>
            {
                createHeader(name, "F_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                    new DataPacketEntityDescriptor[]
                    {
                        new DataPacketEntityDescriptor("STAT", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("SGY1", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("SRO1", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("ANG1", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("TEM1", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("SGY2", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("SRO2", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("ANG2", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("TEM2", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                        new DataPacketEntityDescriptor("ADSG", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("ADH1", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("ADH2", dpedPos.Add(2).DValue, dpedPos.PreviousValue, false),
                        new DataPacketEntityDescriptor("ADTM", dpedPos.Add(2).DValue, dpedPos.PreviousValue, true),
                    }),
            }.ToArray();

            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            registerStandardCalibrationFile(CalibrationFileEntity.CreateConstantsTable("dTe", DataTypes.INT16, new short[] { 0 }));

            pos.Value = 0;
            var v01TemperatureCalibrationFileForRotationSensorTarget
                = new FileDescriptorsTarget(FileType.TEMPERATURE_CALIBRATION, "01", id);
            var v01TemperatureCalibrationFileForRotationSensor = new Enumerable<FED>
            {
                createHeader(name, "T_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Калибровки", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY, 
                    new CalibrationFileEntity[] 
                    {
                        CalibrationFileEntity.CreateArray("AH1", DataTypes.UINT16, Enumerable.Repeat((ushort)32498, 156).ToArray()),
                        CalibrationFileEntity.CreateArray("AH2", DataTypes.UINT16, Enumerable.Repeat((ushort)32715, 156).ToArray()),
                        CalibrationFileEntity.CreateArray("ASG", DataTypes.UINT16, Enumerable.Repeat((ushort)32809, 156).ToArray()),
                        CalibrationFileEntity.CreateArray("Kgy", DataTypes.FLOAT, Enumerable.Repeat((float)0.12947888, 156).ToArray())
                    }),
            }.ToArray();

            descriptors.Add(v01DataPacketConfigurationFileForRotationSensorTarget, new FileStructure(v01DataPacketConfigurationFileForRotationSensor));
            descriptors.Add(v01TemperatureCalibrationFileForRotationSensorTarget, new FileStructure(v01TemperatureCalibrationFileForRotationSensor));

            #endregion

            #region Izmeritel 0b00000100

            name = "Izmeritl";
            id = RUSDeviceId.IZMERITEL;
            pos.Value = 0;

            registerStandard(FileType.FACTORY_SETTINGS);

            pos.Value = 0;
            var v01DataPacketConfigurationFileForIzmeritelTarget
                = new FileDescriptorsTarget(FileType.DATA_PACKET_CONFIGURATION, "01", id);
            var v01DataPacketConfigurationFileForIzmeritel = new Enumerable<FED>
            {
                createHeader(name, "F_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                    new DataPacketEntityDescriptor[]
                    {
                        new DataPacketEntityDescriptor("TimP", 2, 0x07, false),
                        new DataPacketEntityDescriptor("DD1_", 2, 0x09, false),
                        new DataPacketEntityDescriptor("DD2_", 2, 0x0B, false),
                        new DataPacketEntityDescriptor("TDD1", 2, 0x0D, false),
                        new DataPacketEntityDescriptor("TDD2", 2, 0x0F, false),
                        new DataPacketEntityDescriptor("TACD", 2, 0x11, false),
                        new DataPacketEntityDescriptor("+INC", 2, 0x13, false),
                        new DataPacketEntityDescriptor("-INC", 2, 0x14, false),
                        new DataPacketEntityDescriptor("GK__", 2, 0x15, false),
                        new DataPacketEntityDescriptor("TIME", 2, 0x17, false),
                        new DataPacketEntityDescriptor("URTC", 2, 0x19, false),
                        new DataPacketEntityDescriptor("REZ_", 2, 0x1B, false),
                        new DataPacketEntityDescriptor("SerN", 2, 0x23, false),
                    }),
            }.ToArray();

            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            registerStandard(FileType.CALIBRATION);

            descriptors.Add(v01DataPacketConfigurationFileForIzmeritelTarget, new FileStructure(v01DataPacketConfigurationFileForIzmeritel));

            #endregion

            #region Inclinometr 0b00000011

            name = "INCL____";
            id = RUSDeviceId.INCLINOMETR;
            pos.Value = 0;
            dpedPos.Value = 1;

            registerStandard(FileType.FACTORY_SETTINGS);

            pos.Value = 0;
            var v01DataPacketConfigurationFileForInclinometrTarget
                = new FileDescriptorsTarget(FileType.DATA_PACKET_CONFIGURATION, "01", id);
            var v01DataPacketConfigurationFileForInclinometr = new Enumerable<FED>
            {
                createHeader("INCL____", "F_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                    new DataPacketEntityDescriptor[]
                    {
                        new DataPacketEntityDescriptor("STAT", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("INC_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("GTF_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("MTF_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("AZI_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("DIP_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("GTOT", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("BTOT", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("GCNT", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("Taks", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("Tmag", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("XGrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("YGrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("ZGrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("XMrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("YMrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("ZMrp", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("XGad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("YGad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("ZGad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("XMad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("YMad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("ZMad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x8F),
                        new DataPacketEntityDescriptor("TGad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("Tmad", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("UHV_", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("UHAL", dpedPos.Add(2).DValue, dpedPos.PreviousValue, 0x10),
                        new DataPacketEntityDescriptor("RSRV", dpedPos.Add(2).DValue, 59, 0x10),
                    }),
            }.ToArray();

            registerStandard(FileType.FLASH_DATA_PACKET_CONFIGURATION);

            registerStandard(FileType.CALIBRATION);

            pos.Value = 0;
            var v01TemperatureCalibrationFileForInclinometrTarget
                = new FileDescriptorsTarget(FileType.TEMPERATURE_CALIBRATION, "01", id);
            var v01TemperatureCalibrationFileForInclinometr = new Enumerable<FED>
            {
                createHeader(name, "T_"),
                new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                new FED("Калибровки", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY, new CalibrationFileEntity[]
                {
                    CalibrationFileEntity.CreateLinearInterpolationTable("G1x", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                    CalibrationFileEntity.CreateLinearInterpolationTable("G1y", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                    CalibrationFileEntity.CreateLinearInterpolationTable("G1z", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                    CalibrationFileEntity.CreateLinearInterpolationTable("M1x", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                    CalibrationFileEntity.CreateLinearInterpolationTable("M1y", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                    CalibrationFileEntity.CreateLinearInterpolationTable("M1z", DataTypes.FLOAT, 140.Range().Select(_ => (0F, 1F)).ToArray()),
                 }),
            }.ToArray();

            descriptors.Add(v01DataPacketConfigurationFileForInclinometrTarget, new FileStructure(v01DataPacketConfigurationFileForInclinometr));
            descriptors.Add(v01TemperatureCalibrationFileForInclinometrTarget, new FileStructure(v01TemperatureCalibrationFileForInclinometr));

            #endregion

            Descriptors = descriptors.AsReadOnly();

            //////////////////////////////////////////////////////

            void registerStandard(FileType fileType)
            {
                pos.Value = 0;
                var fileMark = fileType.GetAttribute<FileTypeInfoAttribute>().FileTypePointerName;
                var target = new FileDescriptorsTarget(fileType, "01", id);
                FED[] file = null;
                switch (fileType)
                {
                    case FileType.CALIBRATION:
                        registerStandardCalibrationFile(new CalibrationFileEntity[0]);
                        return;
                    case FileType.FACTORY_SETTINGS:
                        file = new Enumerable<FED>
                        {
                            createHeader(name, fileMark),
                            new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                            new FED("Текстовый файл", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.ASCII_STRING, "??"),
                        }.ToArray();
                        break;
                    case FileType.DATA_PACKET_CONFIGURATION:
                        file = new Enumerable<FED>
                        {
                            createHeader(name, fileMark),
                            new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                            new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                                new DataPacketEntityDescriptor[]
                                {

                                }),
                        }.ToArray();
                        break;
                    case FileType.FLASH_DATA_PACKET_CONFIGURATION:
                        file = new Enumerable<FED>
                        {
                            createHeader(name, fileMark),
                            new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                            new FED("Массив описаний пакета данных", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY,
                                new DataPacketEntityDescriptor[]
                                {

                                }),
                        }.ToArray();
                        break;

                    default:
                        throw new NotSupportedException();
                }

                descriptors.Add(target, new FileStructure(file));
            }
            void registerStandardCalibrationFile(params CalibrationFileEntity[] coefficients)
            {
                pos.Value = 0;
                var fileMark = FileType.CALIBRATION.GetAttribute<FileTypeInfoAttribute>().FileTypePointerName;
                var target = new FileDescriptorsTarget(FileType.CALIBRATION, "01", id);
                var file = new Enumerable<FED>
                {
                    createHeader(name, fileMark),
                    new FED("Резерв", pos, pos.Add(14).DValue, DataEntityFormat.BYTE_ARRAY, new byte[14]),
                    new FED("Калибровки", pos, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY, coefficients),
                }.ToArray();

                descriptors.Add(target, new FileStructure(file));
            }

            FED[] createHeader(string deviceShortName, string fileTypeMark)
            {
                return new FED[]
                {
                    new FED("Краткое название прибора", pos, pos.Add(8).DValue, DataEntityFormat.ASCII_STRING, deviceShortName),
                    new FED("Модификация", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, "??"),
                    new FED("Версия ПО или пакета данных", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, "01"),
                    new FED("Дата составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, "DD"),
                    new FED("Месяц составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, "MM"),
                    new FED("Год составления файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, "YY"),
                    new FED("Указатель типа файла", pos, pos.Add(2).DValue, DataEntityFormat.ASCII_STRING, fileTypeMark),
                    new FED("Серийный номер прибора", pos, pos.Add(2).DValue, DataEntityFormat.UINT16, DEFAULT_SERIAL),
                };
            }
        }

        public static IEnumerable<IDataEntity> SetBurnDate(IEnumerable<IDataEntity> file, DateTime date)
        {
            var year = toString(date.Year);
            var month = toString(date.Month);
            var day = toString(date.Day);

            string toString(int value)
            {
                return value.ToString().PadLeft(2, '0').TakeLast(2).Aggregate();
            }

            var yearEName = BaseFileTemplate.Find(v => v.EntityType == FileEntityType.BURN_YEAR).Value.Name;
            var monthEName = BaseFileTemplate.Find(v => v.EntityType == FileEntityType.BURN_MONTH).Value.Name;
            var dayEName = BaseFileTemplate.Find(v => v.EntityType == FileEntityType.BURN_DAY).Value.Name;

            foreach (var e in file)
            {
                var position = new[] { yearEName, monthEName, dayEName }.Find(e.Descriptor.Name);
                if (position.Found)
                {
                    var value = new[] { year, month, day }[position.Index];
                    yield return new DataEntity(value, e.Descriptor.Serialize(value), e.Descriptor);
                }
                else
                {
                    yield return e;
                }
            }
        }

        public static IEnumerable<IDataEntity> SetSerialNumber(IEnumerable<IDataEntity> file, int serialNumber)
        {
            var serialNumberEName = BaseFileTemplate
                .Find(v => v.EntityType == FileEntityType.SERIAL_NUMBER)
                .Value.Name;
            foreach (var e in file)
            {
                if (serialNumberEName == e.Descriptor.Name)
                {
                    var value = (ushort)serialNumber;
                    yield return new DataEntity(value, e.Descriptor.Serialize(value), e.Descriptor);
                }
                else
                {
                    yield return e;
                }
            }
        }

        public static IDataEntity GetFileEntity(IEnumerable<IDataEntity> file, FileEntityType fileEntityType)
        {
            var eName = BaseFileTemplate
                .Find(v => v.EntityType == fileEntityType)
                .Value.Name;
            foreach (var e in file)
            {
                if (eName == e.Descriptor.Name)
                {
                    return e;
                }
            }

            throw new ArgumentException();
        }

        public static IEnumerable<IDataEntity> SetFileEntity(
            IEnumerable<IDataEntity> file, 
            FileEntityType fileEntityType, 
            object value)
        {
            var eName = BaseFileTemplate
                .Find(v => v.EntityType == fileEntityType)
                .Value.Name;
            foreach (var e in file)
            {
                if (eName == e.Descriptor.Name)
                {
                    yield return new DataEntity(value, e.Descriptor.Serialize(value), e.Descriptor);
                }
                else
                {
                    yield return e;
                }
            }
        }

        public static IEnumerable<IDataEntity> ExtractHeader(IEnumerable<IDataEntity> file)
        {
            var entities = BaseFileTemplate.Select(d => d.Name).ToArray();

            return file.Where(e => entities.Contains(e.Descriptor.Name));
        }

        /// <summary>
        /// Doesn't change <paramref name="file"/>'s FileTypePointer field!
        /// </summary>
        /// <param name="file"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static IEnumerable<IDataEntity> MergeHeader(
            IEnumerable<IDataEntity> file, 
            IEnumerable<IDataEntity> header)
        {
            foreach (var entity in file)
            {
                var descriptor = BaseFileTemplate.Find(e => e.Name == entity.Descriptor.Name);
                if (descriptor.Found && 
                    descriptor.Value.EntityType != FileEntityType.FILE_TYPE_POINTER)
                {
                    yield return header.Find(e => e.Descriptor.Name == descriptor.Value.Name).Value;
                }
                else
                {
                    yield return entity;
                }
            }
        }
    }
}