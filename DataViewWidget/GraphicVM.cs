using DataViewExports;
using System;

namespace DataViewWidget
{
    public class GraphicVM
    {
        public GraphicVM(GraphicViewSetingVM settingsVM, IPointsStorageProvider sourceVM)
        {
            Settings = settingsVM ?? throw new ArgumentNullException(nameof(settingsVM));
            SourceVM = sourceVM ?? throw new ArgumentNullException(nameof(sourceVM));
        }

        public GraphicViewSetingVM Settings { get; }
        public IPointsStorageProvider SourceVM { get; }
    }
}
