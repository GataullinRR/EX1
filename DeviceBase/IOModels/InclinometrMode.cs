using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Attributes;

namespace DeviceBase.IOModels
{
    public enum InclinometrMode : ushort
    {
        [InclinometrModeInfo(0b11 << 4, 0)]
        WORKING = 0x0000,
        [InclinometrModeInfo(0b01 << 4, 0b01 << 4)]
        TEMPERATURE_CALIBRATION = 0x00FF,
        [InclinometrModeInfo(0b10 << 4, 0b10 << 4)]
        ANGULAR_CALIBRATION = 0x0FFF,
    }
}
