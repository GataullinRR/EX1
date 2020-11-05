using DataViewExports;
using MVVMUtilities.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataViewWidget
{
    public class GraphicViewSetingVM : INotifyPropertyChanged, IGraphicViewSetingVM
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsAutoscrollSupported { get; set; } = true;
        public bool IsClearSupported { get; set; } = true;
        public bool IsAutoscaleSupported { get; set; } = true;

        public bool IsAutoscaleEnabled { get; set; } = true;
        public bool IsAutoscrollEnabled { get; set; } = true;
        public int YMin { get; set; }
        public Int32Marshaller YAxisLength { get; set; } = new Int32Marshaller(1000, v => v > 10); // No max amount of point limit anymore!
        public bool? CurveGroupShow { get; set; } = true;

        public GraphicViewSetingVM()
        {
            YAxisLength.ModelValueChanged += YAxisLength_ModelValueChanged;
        }

        void YAxisLength_ModelValueChanged(object sender, ValueChangedEventArgs<int> e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YAxisLength)));
        }
    }
}
