using Common;
using Calibrators.Models;
using MVVMUtilities.Types;
using System.ComponentModel;
using WPFControls;
using System.Linq;

namespace Calibrators.ViewModels
{
    internal class PulseDurationVM : INotifyPropertyChanged
    {
        readonly BusyObject _isBusy;
        readonly ShockSensorCalibrator _calibrator;
        BoolOption _previousCheckedOption;
        BoolOption _lastFallbackedOption;

        public event PropertyChangedEventHandler PropertyChanged;

        public EnhancedObservableCollection<BoolOption> ImpulseDurations { get; }
            = new EnhancedObservableCollection<BoolOption>()
            {
                        new BoolOption() { IsChecked=true, OptionName = "2", Tag = 2D},
                        new BoolOption() { OptionName = "1", Tag = 1D},
                        new BoolOption() { OptionName = "0.5", Tag = 0.5D},
                        new BoolOption() { OptionName = "0.25", Tag = 0.25D}
            };
        public double SelectedPulseDuration => (double)ImpulseDurations.Single(o => o.IsChecked).Tag;

        public PulseDurationVM(BusyObject isBusy, ShockSensorCalibrator calibrator)
        {
            _isBusy = isBusy;
            _calibrator = calibrator;
            _previousCheckedOption = ImpulseDurations.Single(o => o.IsChecked);
        }

        public async void OptionCheckedAsync(BoolOption newOption)
        {
            using (_isBusy.BusyMode)
            {
                // Because i dont want user to shift between two options till 'Yes' will be pressed...
                if (newOption != _lastFallbackedOption)
                {
                    var doChange = await UserInteracting.RequestAcknowledgementAsync("Смена длительности импульса", "Данные предыдущих замеров будут стерты.-NL-NLПродолжить?");
                    if (doChange)
                    {
                        _calibrator.DiscardShockCalibration();
                        _lastFallbackedOption = null;
                    }
                    else
                    {
                        newOption.IsChecked = false;
                        _lastFallbackedOption = _previousCheckedOption;
                        _previousCheckedOption.IsChecked = true; // this line triggers method to run again, so it should be the last
                    }

                    _previousCheckedOption = ImpulseDurations.Single(o => o.IsChecked);
                }
            }
        }
    }
}
