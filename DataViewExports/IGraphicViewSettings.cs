using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataViewExports
{
    public interface IGraphicViewSetingVM : INotifyPropertyChanged
    {
        bool IsAutoscaleSupported { get; set; }
        bool IsAutoscrollSupported { get; set; }
        bool IsClearSupported { get; set; }

        bool IsAutoscaleEnabled { get; set; }
        bool IsAutoscrollEnabled { get; set; }
        Int32Marshaller YAxisLength { get; set; }
        bool? CurveGroupShow { get; set; }
    }
}
