using System;

namespace DeviceBase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class SizeAttribute : Attribute
    {
        public int Size { get; }

        public SizeAttribute(int sizeInBytes)
        {
            Size = sizeInBytes;
        }
    }
}
