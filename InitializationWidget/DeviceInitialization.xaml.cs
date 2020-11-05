using Common;
using DeviceBase.Devices;
using InitializationExports;
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
using Utilities;
using WidgetHelpers;

namespace InitializationWidget
{
    /// <summary>
    /// Interaction logic for DeviceInitialization.xaml
    /// </summary>
    public partial class DeviceInitialization : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DeviceInitializationVM Model { get; private set; }

        DeviceInitialization()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer serviceProvider)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DeviceInitialization()
                {
                    Model = new DeviceInitializationVM(
                        serviceProvider.ResolveSingle<IRUSDevice>(),
                        serviceProvider.ResolveSingle<BusyObject>(), 
                        new WriteFilesByDefaultVM(
                            serviceProvider.ResolveSingle<IRUSDevice>(), 
                            serviceProvider.ResolveSingle<BusyObject>())),
                    FunctionId = new WidgetIdentity("Инициализация устройства",
                        serviceProvider.ResolveSingle<string>(),
                        activationScope)
                };

                widget.Visibility = Visibility.Collapsed;
                serviceProvider.Register<IDeviceInitializationVM>(widget.Model);
                serviceProvider.Register<IWidget>(widget);
            }
        }
    }
}
