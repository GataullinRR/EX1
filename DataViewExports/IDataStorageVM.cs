using MVVMUtilities.Types;
using WPFUtilities.Types;

namespace DataViewExports
{
    public interface IDataStorageVM : IPointsStorageProvider
    {
        EnhancedObservableCollection<object> DTOs { get; }
        ActionCommand Clear { get; }
    }
}
