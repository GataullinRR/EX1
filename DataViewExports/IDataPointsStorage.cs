using DeviceBase.Models;
using MVVMUtilities.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataViewExports
{
    public interface IDataPointsStorage
    {
        ISlimEnhancedObservableCollection<ICurveInfo> CurveInfos { get; }
        ISlimEnhancedObservableCollection<IPointsRow> PointsRows { get; }
    }
}
