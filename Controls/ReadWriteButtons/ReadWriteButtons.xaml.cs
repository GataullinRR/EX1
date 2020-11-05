using MVVMUtilities.Types;
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
using WPFUtilities.Types;

namespace Controls
{
    /// <summary>
    /// Interaction logic for ReadWriteButtons.xaml
    /// </summary>
    public partial class ReadWriteButtons : UserControl
    {
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(ReadWriteButtons), new PropertyMetadata(""));

        public ICommand ReadCommand
        {
            get { return (ICommand)GetValue(ReadCommandProperty); }
            set { SetValue(ReadCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadCommandProperty =
            DependencyProperty.Register("ReadCommand", typeof(ICommand), typeof(ReadWriteButtons), 
                new PropertyMetadata(new ActionCommand(() => { }) { CanBeExecuted = false }));

        public ICommand WriteCommand
        {
            get { return (ICommand)GetValue(WriteCommandProperty); }
            set { SetValue(WriteCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WriteCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WriteCommandProperty =
            DependencyProperty.Register("WriteCommand", typeof(ICommand), typeof(ReadWriteButtons), 
                new PropertyMetadata(new ActionCommand(() => { }) { CanBeExecuted = false }));

        public ReadWriteButtons()
        {
            InitializeComponent();
        }
    }
}
