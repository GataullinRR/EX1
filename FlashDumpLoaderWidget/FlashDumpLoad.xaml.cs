using Common;
using DataViewExports;
using DeviceBase.Devices;
using FlashDumpLoaderExports;
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
using Utilities.Types;
using WidgetHelpers;

namespace FlashDumpLoaderWidget
{
    /// <summary>
    /// Interaction logic for FLASHDumpLoad.xaml
    /// </summary>
    public partial class FlashDumpLoad : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; internal set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public FlashDumpLoadVM Model { get; internal set; }

        FlashDumpLoad()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new FlashDumpLoad()
                {
                    Model = new FlashDumpLoadVM(
                        container.ResolveSingle<IRUSDevice>().Id,
                        container.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Загрузка дампа Flash",
                        container.ResolveSingle<string>(),
                        activationScope)
                };
                
                container.Register<IWidget>(widget);
                container.Register<IFlashDumpLoader>(widget.Model);
                container.Register<IFlashDumpDataParserFactory>(FlashDumpDataParserFactory.Instance);
                container.Register<IFlashDumpSaver>(FlashDumpSaverFactory.Instance);
                container.Register<IDataStorageVM>(widget.Model.DataStorageVM, activationScope);
                container.Register<IPointsStorageProvider>(widget.Model, activationScope);
            }
        }
    }
}
