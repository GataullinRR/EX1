using Common;
using DeviceBase.Devices;
using InitializationExports;
using MVVMUtilities.Types;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using TinyConfig;
using WPFUtilities.Types;

namespace InitializationWidget
{
    public class DeviceInitializationVM : IDeviceInitializationVM
    {
        readonly BusyObject _busy;
        readonly DeviceValidator _validator;
        
        public event PropertyChangedEventHandler PropertyChanged;

        public ActionCommand Initialize { get; }
        public bool IsInitialized { get; private set; }
        public WriteFilesByDefaultVM WriteFilesByDefault { get; }

        public DeviceInitializationVM(IRUSDevice device, BusyObject busy, WriteFilesByDefaultVM writeFilesByDefault)
        {
            _busy = busy;
            _validator = new DeviceValidator(device);

            Initialize = new ActionCommand(initialize, _busy);
            WriteFilesByDefault = writeFilesByDefault;

            async Task initialize()
            {
                using (_busy.BusyMode)
                {
                    try
                    {
                        IsInitialized = await _validator.CheckFilesAsync();

                        if (IsInitialized)
                        {
                            Logger.LogOKEverywhere("Устройство инициализировано");
                        }
                        else
                        {
                            Logger.LogErrorEverywhere("Устройство не инициализировано");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorEverywhere("Ошибка инициализации устройства", ex);

                        IsInitialized = false;
                    }
                }
            }
        }
    }
}
