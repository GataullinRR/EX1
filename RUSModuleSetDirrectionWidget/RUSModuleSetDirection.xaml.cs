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
using WidgetHelpers;

namespace RUSModuleSetDirrectionWidget
{
    /// <summary>
    /// Interaction logic for RUSModuleSetDirection.xaml
    /// </summary>
    public partial class RUSModuleSetDirection : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public RUSModuleSetDirectionVM Model { get; private set; }

        RUSModuleSetDirection()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer serviceProvider)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new RUSModuleSetDirection()
                {
                    Model = new RUSModuleSetDirectionVM(
                        serviceProvider.ResolveSingle<IRUSDevice>(),
                        serviceProvider.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Настройки бурения",
                        serviceProvider.ResolveSingle<string>(),
                        activationScope)
                };

                serviceProvider.Register<IWidget>(widget);
            }
        }
    }
}
