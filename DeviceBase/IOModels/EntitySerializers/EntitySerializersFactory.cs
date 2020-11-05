using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;

namespace DeviceBase.IOModels.EntitySerializers
{
    static class EntitySerializersFactory
    {
        static readonly IEntitySerializer[] _serializers = new IEntitySerializer[]
        {
            new PrimitiveEntitySerializer(),
            new CalibrationFileEntitiesArraySerializer(),
            new DataPacketEntitiesDescriptorsArraySerializer()
        };

        public static IEntitySerializer GetSerializer(DataEntityFormat format)
        {
            for (int i = 0; i < _serializers.Length; i++)
            {
                var serializer = _serializers[i];
                if (serializer.SupportedFormats.Contains(format))
                {
                    return serializer;
                }
            }

            throw new NotSupportedException();
        }
    }
}
