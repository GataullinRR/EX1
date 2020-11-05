using DeviceBase.Devices;
using System;

namespace DeviceBase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class InclinometrModeInfoAttribute : Attribute
    {
        readonly ushort _mask;
        readonly ushort _value;

        public InclinometrModeInfoAttribute(ushort mask, ushort value)
        {
            _mask = mask;
            _value = value;
        }

        public bool IsThisStatus(ushort status)
        {
            return (status & _mask) == _value;
        }
    }
}
