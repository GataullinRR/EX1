using Calibrators.ViewModels.Inclinometr;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Calibrators.Views
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [ContentProperty(nameof(Widgets))]
    public partial class CalibrationErrors : UserControl
    {
        public static readonly DependencyProperty WidgetsProperty =
            DependencyProperty.Register("Widgets", typeof(object), typeof(CalibrationErrors), new PropertyMetadata(null));
        public static readonly DependencyProperty VMProperty =
            DependencyProperty.Register("ViewModel", typeof(ICalibrationErrorsVM), typeof(CalibrationErrors), new PropertyMetadata(null));

        public object Widgets
        {
            get { return (object)GetValue(WidgetsProperty); }
            set { SetValue(WidgetsProperty, value); }
        }

        internal ICalibrationErrorsVM ViewModel
        {
            get { return (ICalibrationErrorsVM)GetValue(VMProperty); }
            set { SetValue(VMProperty, value); }
        }

        public CalibrationErrors()
        {
            InitializeComponent();
        }
    }
}
