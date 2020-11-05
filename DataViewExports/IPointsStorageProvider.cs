using System.ComponentModel;

namespace DataViewExports
{
    public interface IPointsStorageProvider : INotifyPropertyChanged
    {
        IDataPointsStorage PointsSource { get; }
    }
}
