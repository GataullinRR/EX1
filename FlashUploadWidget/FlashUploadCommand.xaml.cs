using CommandExports;
using Common;
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

namespace FlashUploadWidget
{
    /// <summary>
    /// Interaction logic for FlashUploadCommand.xaml
    /// </summary>
    public partial class FlashUploadCommand : UserControl, ICommandHandlerWidget
    {
        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.CONTROL;
        public CommandHandlerWidgetSettings Settings => new CommandHandlerWidgetSettings(true, true);
        public ICommandHandlerModel Model { get; private set; }

        FlashUploadCommand()
        {
            InitializeComponent();
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(object activationScope, IDIContainer serviceProvider)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new FlashUploadCommand()
                {
                    Model = new FlashUploadCommandVM(
                        serviceProvider.ResolveSingle<IRUSDevice>(),
                        serviceProvider.ResolveSingle<IFlashDumpDataParserFactory>(),
                        serviceProvider.ResolveSingle<IFlashDumpSaver>(),
                        serviceProvider.ResolveSingle<IFlashDumpLoader>(),
                        serviceProvider.ResolveSingle<BusyObject>()),
                    FunctionId = new WidgetIdentity("Чтение дампа Flash",
                        serviceProvider.ResolveSingle<string>(),
                        activationScope)
                };

                //serviceProvider.Register<IWidget>(widget);
                serviceProvider.Register<ICommandHandlerWidget>(widget, Command.DOWNLOAD_FLASH);
            }
        }
    }
}
