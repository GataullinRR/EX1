using Calibrators;
using Common;
using DataViewExports;
using DeviceBase.Devices;
using DeviceBase.Helpers;
using MVVMUtilities.Types;
using RUSManagingToolExports;
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
using WPFUtilities.Types;

namespace DataRequestWidget
{
    /// <summary>
    /// Interaction logic for DataRequest.xaml
    /// </summary>
    public partial class DataRequest : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; internal set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DataRequestVM Model { get; internal set; }

        DataRequest()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer container)
        {
            // Hack to allow IDataProvider to resolve on time 
            // (we allow other widgets to be resolved and register the dependency required, while this one is in waiting state)
            return ResolutionStepResult.WAITING_FOR_SERVICE
                .Repeat(1) // skip specified amount of steps 
                .Concat(Helpers.CoroutineEnumerable(instantiate))
                .GetEnumerator();

            void instantiate()
            {
                var dr = new DeviceDataRequestVM(
                    container.ResolveSingle<IRUSDevice>(), 
                    container.ResolveSingle<BusyObject>(),
#warning will not return all providers if at the time of resolving not all of them were registered
                    container.TryResolveAll<IDataProvider>()?.ToArray() ?? new IDataProvider[0]);
                var widget = new DataRequest()
                {
                    Model = new DataRequestVM(container.ResolveSingle<IRUSDevice>(),
                        dr,
                        new DeviceDataAutorequestVM(dr.GetDataRequest, container.ResolveSingle<BusyObject>())),
                    FunctionId = new WidgetIdentity("Опрос",
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                container.Register<IWidget>(widget);
                container.Register<IDeviceHandler>(widget.Model);
                container.Register<IDataStorageVM>(widget.Model.RequestVM.DeviceDataVM, activationScope);
                container.Register<IPointsStorageProvider>(widget.Model.RequestVM.DeviceDataVM, activationScope);
            }
        }
    }
}
