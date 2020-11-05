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

namespace FilesWidget
{
    /// <summary>
    /// Interaction logic for DeviceFiles.xaml
    /// </summary>
    public partial class DeviceFiles : UserControl, IWidget
    {
        static DeviceFiles()
        {
            // Otherwise Controls.dll wont be copied to the output directory
            // See: https://stackoverflow.com/questions/15816769/dependent-dll-is-not-getting-copied-to-the-build-output-folder-in-visual-studio
            Controls.ReadWriteButtons _;
        }

        public WidgetIdentity FunctionId { get; internal set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public DeviceFilesVM Model { get; internal set; }

        DeviceFiles()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DeviceFiles()
                {
                    Model = new DeviceFilesVM(container.ResolveSingle<IRUSDevice>(), 
                        container.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Файлы",
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                widget.Visibility = Visibility.Collapsed;
                container.Register<IWidget>(widget);
            }
        }
    }
}
