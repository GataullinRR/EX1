using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace Calibrators.ViewModels.Inclinometr
{
    internal interface ICalibrationErrorsVM : INotifyPropertyChanged
    {
        bool IsLoading { get; }
        PlotVM[] Plots { get; }
        Dictionary<string, NameValuePair<double>[]> ParametersGroups { get; }
    }

    internal abstract class CalibrationErrorsVMBase<T> : ICalibrationErrorsVM
        where T : Models.ICalibrator
    {
        readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
        protected readonly T _calibrator;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsLoading { get; protected set; }
        public abstract PlotVM[] Plots { get; }
        public abstract Dictionary<string, NameValuePair<double>[]> ParametersGroups { get; }

        public CalibrationErrorsVMBase(T calibrator)
        {
            _calibrator = calibrator;

            var oldValue = false;
            _calibrator.PropertyChanged += async (o, e) =>
            {
                var newIsEnabled = isEnabled();
                if (newIsEnabled ^ oldValue)
                {
                    oldValue = newIsEnabled;

                    if (newIsEnabled)
                    {
                        await safeReloadAsync();
                    }
                    else
                    {
                        Clear();
                    }
                }
            };

            PropertyChanged += CalibrationErrorsVMBase_PropertyChanged;
        }

        private void CalibrationErrorsVMBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        protected async Task safeReloadAsync()
        {
            if (isEnabled())
            {
                using (await _locker.AcquireAsync())
                using (new FlagInverseAction(true, v => IsLoading = v))
                {
                    try
                    {
                        Logger.LogInfoEverywhere("Обновление графика отклонений");

                        await ReloadAsync();

                        Logger.LogOKEverywhere("График отклонений обновлен");
                    }
                    catch (Exception ex)
                    {
                        Clear();

                        Logger.LogErrorEverywhere("Ошибка во время обновления графика отклонений", ex);
                    }
                }
            }
        }

        protected virtual bool isEnabled()
        {
            return _calibrator.CallibrationCanBeGenerated;
        }

        public abstract Task ReloadAsync();
        public abstract void Clear();
    }
}
