using Calibrators;
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

namespace RUSTelemetryStreamSenderWidget
{
    /// <summary>
    /// Interaction logic for RUSTelemetryStreamSender.xaml
    /// </summary>
    public partial class RUSTelemetryStreamSender : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public RUSTelemetryStreamSenderVM Model { get; private set; }

        RUSTelemetryStreamSender()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new RUSTelemetryStreamSender()
                {
                    Model = new RUSTelemetryStreamSenderVM(
                        container.ResolveSingle<IRUSDevice>(),
                        container.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Отправка кривой давления",
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                container.Register<IDataProvider>(widget.Model);
                container.Register<IWidget>(widget);
            }
        }
    }
}
