using Calibrators.ViewModels.Inclinometr;
using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for TemperatureCalibrationErrorView.xaml
    /// </summary>
    public partial class InclinometrAngularCalibrationErrors : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; set; }
        public Control View => this;
        public WidgetType Type => WidgetType.DATA;
        internal AngularCalibrationErrorsVM ViewModel { get; set; }
        public object Model => ViewModel;

        public InclinometrAngularCalibrationErrors()
        {
            InitializeComponent();
        }
    }
}
