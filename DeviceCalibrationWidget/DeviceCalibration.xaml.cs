using Calibrators;
using Common;
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

namespace CalibrationWidget
{
    /// <summary>
    /// Interaction logic for DeviceCalibration.xaml
    /// </summary>
    public partial class DeviceCalibration : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; internal set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DeviceCalibrationVM Model { get; internal set; }

        DeviceCalibration()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Inspired by coroutines from Unity3D
        /// </summary>
        /// <param name="calibrator"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(ICalibrator calibrator, object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DeviceCalibration()
                {
                    Model = new DeviceCalibrationVM(calibrator,
                        container.ResolveSingle<IDeviceInitializationVM>(),
                        container.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity(calibrator.Model.CalibrationName,
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                widget.View.Visibility = Visibility.Collapsed;

                container.Register<IWidget>(widget);
                container.Register<IDataProvider>(calibrator.Model.DataProvider);
            }
        }
    }
}
