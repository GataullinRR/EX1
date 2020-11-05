using System;
using System.Collections.Generic;

namespace DeviceBase.IOModels
{
    class DataEntity : IDataEntity
    {
        readonly Lazy<IEnumerable<byte>> _rawValue;

        public EntityDescriptor Descriptor { get; }
        /// <summary>
        /// In device's order
        /// </summary>
        public IEnumerable<byte> RawValue => _rawValue.Value;
        /// <summary>
        /// Any type listed in the <see cref="DataEntityFormat"/>
        /// </summary>
        public object Value { get; }

        public DataEntity(object value, EntityDescriptor descriptor)
            : this(value, new Lazy<IEnumerable<byte>>(() => descriptor.Serialize(value)), descriptor)
        {

        }
        public DataEntity(object value, Lazy<IEnumerable<byte>> rawValue, EntityDescriptor descriptor)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            _rawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }
        public DataEntity(object value, IEnumerable<byte> rawValue, EntityDescriptor descriptor)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            _rawValue = new Lazy<IEnumerable<byte>>(() => rawValue) ?? throw new ArgumentNullException(nameof(rawValue));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "NULL";
        }
    }
}
