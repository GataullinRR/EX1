using System;
using System.Collections.Generic;
using System.IO;
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
using UtilitiesStandard;

namespace UpdateManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //var keys = SecurityUtils.CreateNewRSAKeys(4096);
            //File.WriteAllText("D:\\1.txt", keys.PublicAndPrivateKeys);
            //File.WriteAllText("D:\\2.txt", keys.PublicKey);
        }
    }
}
