using Common;
using DataViewExports;
using ExportersExports;
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

namespace ViewSettingsWidget
{
    /// <summary>
    /// Interaction logic for DataViewSettings.xaml
    /// </summary>
    public partial class DataViewSettings : UserControl, IWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DataViewSetingVM Model { get; private set; }

        DataViewSettings()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(bool isStaticData, object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DataViewSettings()
                {
                    Model = new DataViewSetingVM(
                        container.ResolveSingle<IDataStorageVM>(activationScope),
                        container.ResolveSingle<IGraphicViewSetingVM>(activationScope),
                        #warning same issue here
                        container.ResolveAll<ICurvesExporterVM>(activationScope).ToArray()),
                    FunctionId = new WidgetIdentity("Отображение",
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                if (isStaticData)
                {
                    widget.Model.ViewSetingVM.IsAutoscrollEnabled = false;
                    widget.Model.ViewSetingVM.IsAutoscrollSupported = false; // because data from flash are static
                    widget.Model.ViewSetingVM.IsClearSupported = false;
                }

                container.Register<IWidget>(widget);
            }
        }
    }
}
