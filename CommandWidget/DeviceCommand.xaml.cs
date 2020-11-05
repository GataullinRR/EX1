using CommandExports;
using Common;
using DeviceBase.Devices;
using DeviceBase.Helpers;
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
using Utilities.Extensions;
using WidgetHelpers;

namespace CommandWidget
{
    /// <summary>
    /// Interaction logic for DeviceCommands.xaml
    /// </summary>
    public partial class DeviceCommand : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; internal set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DeviceCommandVM Model { get; internal set; }

        DeviceCommand()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(Command command, object activationScope, IDIContainer serviceProvider)
        {
            return ResolutionStepResult.WAITING_FOR_SERVICE.Repeat(3).Concat(Helpers.CoroutineEnumerable(instantiate)).GetEnumerator();

            void instantiate()
            {
                var handler = serviceProvider.TryResolveSingle<ICommandHandlerWidget>(command);
                activationScope = handler?.FunctionId?.ActivationScope ?? activationScope;
                var widgetName = handler?.FunctionId?.Name ?? command.GetInfo().CommandName;
                var widget = new DeviceCommand()
                {
                    Model = new DeviceCommandVM(
                        serviceProvider.ResolveSingle<IRUSDevice>(),
                        command,
                        handler,
                        serviceProvider.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity(widgetName,
                        serviceProvider.ResolveSingle<string>(),
                        activationScope)
                };

                serviceProvider.Register<IWidget>(widget);
            }
        }
    }
}
