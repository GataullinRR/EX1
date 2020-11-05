using Common;
using DeviceBase.Devices;
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

namespace StatusWidget
{
    /// <summary>
    /// Interaction logic for DeviceStatus.xaml
    /// </summary>
    public partial class DeviceStatus : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DeviceStatusVM Model { get; private set; }

        DeviceStatus()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DeviceStatus()
                {
                    Model = new DeviceStatusVM(
                        container.ResolveSingle<IRUSDevice>(),
                        container.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Статус",
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                container.Register<IWidget>(widget);
            }
        }
    }
}
