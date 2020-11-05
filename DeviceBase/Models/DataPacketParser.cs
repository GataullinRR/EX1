using DeviceBase.Devices;
using DeviceBase.IOModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Utilities.Extensions;
using Vectors;

namespace DeviceBase.Models
{
    public class DataPacketParser : IDataPacketParser
    {
        readonly EntityDescriptor[] _descriptors;
        
        public ICurveInfo[] Curves { get; set; }
        public IntInterval RowLength { get; }

        public DataPacketParser(IEnumerable<EntityDescriptor> descriptors)
        {
            _descriptors = descriptors.ToArray();
            RowLength = new IntInterval(_descriptors.Select(d => d.Position + d.Length.Length).Max());
            Curves = descriptors.Select(d => new CurveInfo(d.Name, true)).ToArray();
        }

        public IPointsRow ParseRow(IList<byte> data)
        {
            // tried to optimize
            var entities = EntitiesDeserializer.Deserialize(data, _descriptors).ToArray();
            var points = new double[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                points[i] = (entities[i] as IPointDataEntity)?.Point ?? double.NaN;
            }

            return new PointsRow(points);
        }
    }
}