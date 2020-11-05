using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using MVVMUtilities;
using MVVMUtilities.Types;
using Utilities.Extensions;
using RUSManagingTool.Models;
using System.IO.Ports;
using System.Threading;
using RUSManagingTool.ViewModels;
using Common;
using WPFUtilities.Extensions;
using Ninject;
using DeviceBase.IOModels;
using WPFUtilities.Converters;
using WidgetsCompositionRoot;

namespace RUSManagingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly MainVM _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = (MainVM)Resources["MainVM"];
            DataContext = _viewModel;

            {
                var version = Assembly.GetEntryAssembly().GetName().Version;
                var versionString = $"{version.Major}.{version.Minor}.{version.Build}:{version.Revision}";
                Title = string.Format(Title, versionString);
                //Logger.LogInfo(null, $"Программа запущена. V{versionString}");
            }

            tc_DataWidgets.SelectionChanged += (o, e) =>
            {
                foreach (var dataWidget in _viewModel.DevicesVM.SelectedDevice.Widgets.DataWidgets)
                {
                    foreach (var boundWidget in _viewModel.DevicesVM.SelectedDevice.Widgets.ControlWidgets
                        .Where(cw => cw.FunctionId.ActivationScope != null && cw.FunctionId.ActivationScope == dataWidget.FunctionId.ActivationScope))
                    {
                        boundWidget.View.Tag = new object(); // Anything not null indicates that the widget is collapsed
                    }
                }

                if (tc_DataWidgets.SelectedIndex != -1)
                {
                    var view = tc_DataWidgets.Items[tc_DataWidgets.SelectedIndex]
                        .To<TabItem>()
                        .Content;
                    var widgetId = _viewModel.DevicesVM.SelectedDevice.Widgets.DataWidgets
                        .Single(dw => dw.View == view)
                        .FunctionId
                        .ActivationScope;
                    var boundControlWidgets = _viewModel.DevicesVM.SelectedDevice.Widgets.ControlWidgets
                        .Where(cw => cw.FunctionId.ActivationScope != null && cw.FunctionId.ActivationScope == widgetId);
                    foreach (var boundControlWidget in boundControlWidgets)
                    {
                        boundControlWidget.View.Tag = null; // Make visible
                    }
                }
            };
            
            {
                _viewModel.DevicesVM.PropertyChanged += DevicesVM_PropertyChanged;

#warning subscribing to the same event!
                void DevicesVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(MainVM.DevicesVM.SelectedDevice))
                    {
                        updateDataWidgets();
                       // subscribe();
                    }

                    void updateDataWidgets()
                    {
                        tc_DataWidgets.Items.Clear();
                        foreach (var dataWidget in _viewModel.DevicesVM.SelectedDevice?.Widgets?.DataWidgets.NullToEmpty())
                        {
                            var tab = new TabItem();
                            tab.Header = dataWidget.FunctionId.Name;
                            tab.Content = dataWidget.View;
                            tc_DataWidgets.Items.Add(tab);
                        }
                        tc_DataWidgets.SelectedIndex = 0;
                    }
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !UserInteracting.RequestAcknowledgement("Закрытие программы", $"Несохраненные данные будут потеряны.-NL-NLВсе равно закрыть?");

            base.OnClosing(e);
        }

        readonly Dictionary<object, object> _selectedDevices = new Dictionary<object, object>();
        void cb_RootDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newDeviceObject = getSelected(sender, e);
            if (cb_ChildDevice != null)
            {
                if (_selectedDevices.NotContainsKey(cb_RootDevice.SelectedItem))
                {
                    _selectedDevices[cb_RootDevice.SelectedItem] = ((DeviceSlimVM)cb_RootDevice.SelectedItem).Children[0];
                }

                cb_ChildDevice.SelectedItem = _selectedDevices[cb_RootDevice.SelectedItem];
            }
        }
        void cb_ChildDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newDeviceObject = getSelected(sender, e);
            if (newDeviceObject is DeviceSlimVM newDevice && _viewModel != null)
            {
                _viewModel.DevicesVM.SelectedDevice = newDevice;
                _selectedDevices[cb_RootDevice.SelectedItem] = newDevice;
            }
        }
        object getSelected(object cb, SelectionChangedEventArgs e)
        {
            return e.AddedItems.Count == 0
                ? ((ComboBox)cb).SelectedItem
                : e.AddedItems[0];
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.Visibility = new VisibilityInvertingConverter().Convert(btn.Visibility);
        }
    }
}