using System.Collections.Generic;

namespace DeviceBase.IOModels
{
    public interface IDataEntity
    {
        EntityDescriptor Descriptor { get; }
        IEnumerable<byte> RawValue { get; }
        object Value { get; }
    }
}
