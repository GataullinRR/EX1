using DataViewExports;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataViewWidget
{
    public class DataViewVM
    {
        public GraphicVM GraphicVM { get; }
        public IDataStorageVM DataStorageVM { get; }

        public DataViewVM(IDataStorageVM deviceDataVM)
        {
            GraphicVM = new GraphicVM(new GraphicViewSetingVM(), deviceDataVM);
            DataStorageVM = deviceDataVM ?? throw new ArgumentNullException(nameof(deviceDataVM));
        }
    }
}
