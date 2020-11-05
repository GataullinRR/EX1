using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using MVVMUtilities.Types;
using TinyConfig;
using System.Threading;
using WPFUtilities.Types;

namespace DataRequestWidget
{
    public class DeviceDataAutorequestVM : NotifiableObjectTemplate
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("DeviceDataAutorequestVM");
        readonly static ConfigProxy<int> AUTOREQUEST_INTERVAL_DEFAULT = CONFIG.Read(250);

        readonly ActionCommand _getDataRequest;
        Task _autorequestLoop;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public Int32Marshaller Interval { get; } = new Int32Marshaller(AUTOREQUEST_INTERVAL_DEFAULT, v => v >= 20);
        public bool Autorequest
        {
            get => _propertyHolder.Get(() => false);
            set
            {
                if (value)
                {
                    _cts = new CancellationTokenSource();
                    _autorequestLoop = autorequestLoopAsync(_cts.Token);
                }
                else
                {
                    _cts.Cancel();
                }

                _propertyHolder.Set(value);
            }
        }

        public BusyObject IsBusy { get; }

        public DeviceDataAutorequestVM(ActionCommand getDataRequest, BusyObject isBusy)
        {
            _getDataRequest = getDataRequest ?? throw new ArgumentNullException(nameof(getDataRequest));
            Interval.ValueChanged += Interval_ValueChanged;
            IsBusy = isBusy;

            void Interval_ValueChanged(object sender, EventArgs e)
            {
                AUTOREQUEST_INTERVAL_DEFAULT.Value = Interval.ModelValue;
            }
        }

        async Task autorequestLoopAsync(CancellationToken cancellation)
        {
            using (IsBusy.BusyMode)
            {
                while (true)
                {
                    var period = new PeriodDelay(Interval);
                    await _getDataRequest.ExecuteAsync();
                    try
                    {
                        await period.WaitTimeLeftAsync(cancellation);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellation)
        {
            using (IsBusy.BusyMode)
            {
                Autorequest = false;
                if (_autorequestLoop != null)
                {
                    await _autorequestLoop.CatchOperationCanceledExeption();
                }
            }
        }
    }
}
