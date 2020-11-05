using DeviceBase.Models;
using MVVMUtilities.Types;
using System;

namespace DataViewExports
{
    public class DataPointsSource : IDataPointsStorage
    {
        public ISlimEnhancedObservableCollection<ICurveInfo> CurveInfos { get; }
        public ISlimEnhancedObservableCollection<IPointsRow> PointsRows { get; }

        public DataPointsSource(ISlimEnhancedObservableCollection<ICurveInfo> curveInfos, ISlimEnhancedObservableCollection<IPointsRow> pointsRows)
        {
            CurveInfos = curveInfos ?? throw new ArgumentNullException(nameof(curveInfos));
            PointsRows = pointsRows ?? throw new ArgumentNullException(nameof(pointsRows));
        }
    }
}
