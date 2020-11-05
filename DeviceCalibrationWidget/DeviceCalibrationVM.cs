using CalibrationExports;
using Calibrators;
using Common;
using InitializationExports;
using MVVMUtilities.Types;
using System.ComponentModel;
using System.Threading.Tasks;
using WPFUtilities.Types;

namespace CalibrationWidget
{
    public class DeviceCalibrationVM : INotifyPropertyChanged
    {
        readonly IDeviceInitializationVM _deviceInitialization;
        readonly BusyObject _busy;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICalibrator Calibrator { get; }
        public bool IsLocked { get; set; }

        public ActionCommand Begin { get; }
        public ActionCommand Discard { get; }

        public DeviceCalibrationVM(ICalibrator calibrator, IDeviceInitializationVM deviceInitialization, BusyObject busy)
        {
            Calibrator = calibrator;
            _deviceInitialization = deviceInitialization;
            _busy = busy;

            Begin = new ActionCommand(beginAsync, 
                () => _busy.IsNotBusy && Calibrator.Model.Begin.IsCanExecute, 
                _busy, Calibrator.Model.Begin);
            Discard = Calibrator.Model.Discard;

            async Task beginAsync()
            {
                var isExecuted = await _deviceInitialization.Initialize.ExecuteIfCanBeExecutedAsync();
                if (!isExecuted)
                {
                    Logger.LogErrorEverywhere("Не удалось произвести инициализацию");
                }
                else
                {
                    if (_deviceInitialization.IsInitialized)
                    {
                        await Calibrator.Model.Begin.ExecuteIfCanBeExecutedAsync();
                    }
                    else
                    {
                        Logger.LogErrorEverywhere("Устройство не инициализировано, калибровка запрещена");
                    }
                }
            }
        }
    }
}
