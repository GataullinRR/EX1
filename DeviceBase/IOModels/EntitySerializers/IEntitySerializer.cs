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
    interface IEntitySerializer
    {
        DataEntityFormat[] SupportedFormats { get; }
        IEnumerable<byte> Serialize(DataEntityFormat format, object entity);
        object Deserialize(DataEntityFormat format, IEnumerable<byte> serialized);
        string SerializeToString(DataEntityFormat format, object entity);
        object DeserializeFromString(DataEntityFormat format, string serialized);
    }
}
