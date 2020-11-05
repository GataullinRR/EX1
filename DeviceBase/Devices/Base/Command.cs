using DeviceBase.Attributes;
using DeviceBase.IOModels.Protocols;

namespace DeviceBase.Devices
{
    public enum Command 
    {
        #region ### Common to all devices ###

        [RequestAddressInfo(0b00000000, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.NOT_EXISTS)]
        STATUS,
        [RequestAddressInfo(0b00000010, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.NOT_EXISTS)]
        DATA,
        [RequestAddressInfo(0b00001000, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.FACTORY_SETTINGS)]
        FACTORY_SETTINGS_FILE,
        [RequestAddressInfo(0b00001001, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.DATA_PACKET_CONFIGURATION)]
        DATA_PACKET_CONFIGURATION_FILE,
        [RequestAddressInfo(0b00000001, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.CALIBRATION)]
        CALIBRATION_FILE,
        /// <summary>
        /// See https://github.com/GataullinRR/RUS-ManagingTool/issues/254
        /// </summary>
        [RequestAddressInfo(0b01010001, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.FLASH_DATA_PACKET_CONFIGURATION)]
        FLASH_DATA_PACKET_CONFIGURATION,

        #endregion

        /// <summary>
        /// For <see cref="RUSModule"/>
        /// </summary>
        [RequestAddressInfo(0x74, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_MODULE)]
        KEEP_MTF,
        /// <summary>
        /// For <see cref="RUSModule"/>
        /// </summary>
        [RequestAddressInfo(0x77, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_MODULE)]
        ROTATE_WITH_CONSTANT_SPEED,
        /// <summary>
        /// For <see cref="RUSModule"/>
        /// </summary>
        [RequestAddressInfo(0x75, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_MODULE)]
        DRILL_DIRECTLY,
        /// <summary>
        /// For <see cref="RUSModule"/>
        /// </summary>
        [RequestAddressInfo(0x7E, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_MODULE)]
        TURN_ON_AZIMUTH,
        /// <summary>
        /// For <see cref="RUS_MODULE"/>
        /// </summary>
        [RequestAddressInfo(0b00010000, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_MODULE, 
            IsCommand = true, CommandName = "Настройка ШИМ")]
        PWM_SET,

        /// <summary>
        /// For <see cref="RUSLWDLink"/>
        /// </summary>
        [RequestAddressInfo(0b00001111, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.LWD_LINK, 
            IsCommand = true, CommandName = "Настройки ПБП")]
        PBP_SETTINGS,
        /// <summary>
        /// For <see cref="RUSInclinometr"/> and <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00001010, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.TEMPERATURE_CALIBRATION, RUSDeviceId.INCLINOMETR, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE)]
        TEMPERATURE_CALIBRATION_FILE,
        /// <summary>
        /// For <see cref="RUSInclinometr"/> and <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00001011, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.INCLINOMETR, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE)]
        CALIBRATION_MODE_SET,

        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>, <see cref="RUSInclinometr"/>, <see cref="RUSShockSensor"/>, <see cref="RUSRotationSensor"/>, 
        /// </summary>
        [RequestAddressInfo(0b00000111, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.INCLINOMETR, RUSDeviceId.SHOCK_SENSOR, RUSDeviceId.ROTATIONS_SENSOR, RUSDeviceId.RUS_MODULE,
            IsCommand = true, CommandName = "Синхронизировать часы", CanBeBroadcasted = true)]
        CLOCKS_SYNC,

        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00010100, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE, RUSDeviceId.EMC_MODULE,
            IsCommand = true, CommandName = "Перейти в режим выгрузки Flash")]
        SET_DATA_UNLOAD_MODE,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00111000, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE, RUSDeviceId.EMC_MODULE,
            IsCommand = true, CommandName = "Перевести Flash в рабочий режим")]
        SET_FLASH_WORK_MODE,
        /// <summary>
        /// Reads 4 Gb of flash memory
        /// </summary>
        [RequestAddressInfo(default, ReadResponse.BINARY_FILE, WriteResponse.NOT_EXISTS, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE, RUSDeviceId.EMC_MODULE,
            IsCommand = true, CommandName = "Считать флеш", Protocol = Protocol.FTDI_BOX)]
        DOWNLOAD_FLASH,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00010001, ReadResponse.NOT_EXISTS, WriteResponse.RICH, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE, RUSDeviceId.EMC_MODULE,
            IsCommand = true, CommandName = "Очистить Flash")]
        FLASH_ERASE,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00001111, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 
            IsCommand = true, CommandName = "Установить регистры управления")]
        DEVICE_CONTROL_REGISTERS,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00001100, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, 
            IsCommand = true, CommandName = "Установить регистр накопления")]
        FRAME_ACCUMULATION_TIME,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00111111, ReadResponse.NOT_EXISTS, WriteResponse.CUSTOM_WITH_LENGTH, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE)]
        RETRANSLATE_PACKET,
        /// <summary>
        /// For <see cref="RUSTechnologicalModule"/>
        /// </summary>
        [RequestAddressInfo(0b00010110, ReadResponse.CUSTOM_WITH_LENGTH, WriteResponse.ACKNOWLEDGEMENT, FileType.WORK_MODE, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE)]
        WORK_MODE_FILE,

        [RequestAddressInfo(0b00011111, ReadResponse.NOT_EXISTS, WriteResponse.CUSTOM_WITH_LENGTH, RUSDeviceId.RUS_MODULE)]
        REG_DATA_FRAME,

        [RequestAddressInfo(0b00111110, ReadResponse.RICH, WriteResponse.NOT_EXISTS, RUSDeviceId.RUS_TECHNOLOGICAL_MODULE, RUSDeviceId.RUS_MODULE)]
        PING,

        /// <summary>
        /// For <see cref="RUSEMCModule"/>
        /// </summary>
        [RequestAddressInfo(0b00011000, ReadResponse.NOT_EXISTS, WriteResponse.ACKNOWLEDGEMENT, RUSDeviceId.EMC_MODULE, IsCommand = true, CommandName = "Перейти в режим тест")]
        SET_TEST_MODE,
    }
}
