using Calibrators.ViewModels;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Calibrators.Views
{
    /// <summary>
    /// Interaction logic for ShockSensorCalibration.xaml
    /// </summary>
    public partial class ShockSensorCalibration : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        internal ShockSensorCalibrationVM ViewModel { get; set; }
        public object Model => ViewModel;

        public ShockSensorCalibration()
        {
            InitializeComponent();

            os_PulseDuration.Checked += Os_PulseDuration_Checked;
        }

        void Os_PulseDuration_Checked(object sender, WPFControls.OptionCheckedEventArgs e)
        {
            ViewModel.PulseDurationVM.OptionCheckedAsync(e.Option);
        }
    }
}
