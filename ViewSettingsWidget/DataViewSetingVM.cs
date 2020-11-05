using DataViewExports;
using ExportersExports;
using System;
using System.ComponentModel;

namespace ViewSettingsWidget
{
    public class DataViewSetingVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IGraphicViewSetingVM ViewSetingVM { get; }
        public IDataStorageVM StorageVM { get; }
        public ICurvesExporterVM[] Exporters { get; }

        public DataViewSetingVM(IDataStorageVM storageVM, IGraphicViewSetingVM viewSetingVM, ICurvesExporterVM[] exporters)
        {
            StorageVM = storageVM ?? throw new ArgumentNullException(nameof(storageVM));
            ViewSetingVM = viewSetingVM ?? throw new ArgumentNullException(nameof(viewSetingVM));
            Exporters = exporters ?? throw new ArgumentNullException(nameof(exporters));
        }
    }
}
