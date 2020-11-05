using DataViewExports;
using DeviceBase.Models;
using MVVMUtilities.Types;
using System;
using System.ComponentModel;
using WPFUtilities.Types;

namespace FlashDumpLoaderWidget
{
    class FlashDataStorageVM : IDataStorageVM
    {
        readonly IPointsStorageProvider _dataPointsStorageProvider;

        public event PropertyChangedEventHandler PropertyChanged;

        // Do not show table for data from flash
        public EnhancedObservableCollection<object> DTOs { get; } = null;
        public ActionCommand Clear { get; } = new ActionCommand(() => { });
        public IDataPointsStorage PointsSource => _dataPointsStorageProvider.PointsSource 
            ?? new DataPointsSource(new EnhancedObservableCollection<ICurveInfo>(), new EnhancedObservableCollection<IPointsRow>());

        public FlashDataStorageVM(IPointsStorageProvider pointsSource)
        {
            _dataPointsStorageProvider = pointsSource ?? throw new ArgumentNullException(nameof(pointsSource));

            _dataPointsStorageProvider.PropertyChanged += PointsSource_PropertyChanged;
        }

        void PointsSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PointsSource)));
        }
    }
}
