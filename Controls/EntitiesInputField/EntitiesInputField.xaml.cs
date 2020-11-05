using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Controls
{
    /// <summary>
    /// Interaction logic for EntitiesInputField.xaml
    /// </summary>
    public partial class EntitiesInputField : UserControl
    {
        public ObservableCollection<ICommandEntityVM> Entities
        {
            get { return (ObservableCollection<ICommandEntityVM>)GetValue(EntitiesProperty); }
            set { SetValue(EntitiesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Entities.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EntitiesProperty =
            DependencyProperty.Register("Entities", typeof(ObservableCollection<ICommandEntityVM>), typeof(EntitiesInputField), new PropertyMetadata(null));

        public EntitiesInputField()
        {
            InitializeComponent();
        }
    }
}
