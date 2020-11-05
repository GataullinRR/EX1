using DeviceBase.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RUSTelemetryStreamSenderExports
{
    public delegate void DecoratedDataRowAquiredDelegate(IEnumerable<ViewDataEntity> dataRow, RowDecoration decoration);

    public enum RowDecoration
    {
        NONE = 0,
        LINE = 100,
    }
}