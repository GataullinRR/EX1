using Common;
using DeviceBase.Models;
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
using System.Windows.Shapes;
using Utilities.Types;

namespace Exporters.Las
{
    /// <summary>
    /// Interaction logic for LasExportSettingsWindow.xaml
    /// </summary>
    public partial class LasExportWindow : Window
    {
        public LasExportMainVM MainVM { get; }

        public LasExportWindow(IList<ICurveInfo> infos, IList<IPointsRow> pointsRows)
        {
            MainVM = new LasExportMainVM(pointsRows, infos);

            InitializeComponent();

            ShowInTaskbar = false;
            Owner = Application.Current.MainWindow;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (MainVM.Cancel.IsCanExecute)
            {
                e.Cancel = true;
                MainVM.TryCancelExportAsync().ContinueWith(t =>
                {
                    Close();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            base.OnClosing(e);
        }
    }
}
