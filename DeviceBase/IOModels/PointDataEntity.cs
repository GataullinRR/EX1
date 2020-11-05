using System;
using System.Collections.Generic;

namespace DeviceBase.IOModels
{
    class PointDataEntity : DataEntity, IPointDataEntity
    {
        public double Point { get; }

        public PointDataEntity(object value, Lazy<IEnumerable<byte>> rawValue, EntityDescriptor descriptor)
            : base(value, rawValue, descriptor)
        {
            Point = (double)Convert.ChangeType(value, typeof(double));
        }
        public PointDataEntity(object value, IEnumerable<byte> rawValue, EntityDescriptor descriptor)
            : base(value, rawValue, descriptor)
        {
            Point = (double)Convert.ChangeType(value, typeof(double));
        }
    }
}
