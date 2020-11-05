using System;

namespace DeviceBase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class DefaultEntityValueAttribute : Attribute
    {
        public object Value { get; }

        public DefaultEntityValueAttribute(object value)
        {
            Value = value;
        }
        public DefaultEntityValueAttribute(Type elementType)
        {
            Value = Array.CreateInstance(elementType, 0);
        }
    }
}
