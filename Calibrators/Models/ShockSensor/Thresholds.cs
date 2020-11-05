using System.ComponentModel;

namespace Calibrators.Models
{
    class Thresholds : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double G50 { get; set; }
        public double G100 { get; set; }
        public double G200 { get; set; }
        public double G300 { get; set; }
    }
}
