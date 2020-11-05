using Common;
using DeviceBase.IOModels;
using FTD2XXSerialPort;
using IOBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using static FTD2XXSerialPort.FTDI;

namespace FTD2XXSerialPort
{
    public class FTDIInterface : IInterface
    {
        const string PORT_PREFIX = "USB";

        readonly FTDI _ftdi;
        readonly TaskHolder _portUpdater;

        public string PortName { get; private set; }
        public IPipe Pipe { get; private set; }
        public SemaphoreSlim Locker { get; private set; }
#warning does not update
        public bool IsOpen => _ftdi.IsOpen;
        public IEnumerable<string> PortNames { get; private set; } = new string[0];
        public InterfaceDevice CurrentInterfaceDevice => InterfaceDevice.RUS_TECHNOLOGICAL_MODULE_FTDI_BOX;

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        public FTDIInterface(int readTimeout, int writeTimeout, SemaphoreSlim locker)
        {
            Locker = locker;
            _ftdi = new FTDI();
            _portUpdater = new TaskHolder();
            _portUpdater.RegisterAsync(c => updateAwailablePortsLoop(c)).GetAwaiter().GetResult();

            Pipe = new FTDIPipe(_ftdi, readTimeout, writeTimeout);
            openStateCheckingAsyncLoop();
        }

        async void openStateCheckingAsyncLoop()
        {
            await ThreadingUtils.ContinueAtDedicatedThread(CancellationToken.None);
            
            var period = new PeriodDelay(1000);
            while (true)
            {
                if (_openedDevice != null)
                {
                    try
                    {
                        var devices = getAllDevices();
                        var exists = devices.FirstOrDefault(d => d.LocId == _openedDevice.LocId
                            && d.SerialNumber == _openedDevice.SerialNumber
                            && d.Description == _openedDevice.Description) != null;
                        if (!exists)
                        {
                            Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(null, "Ошибка ", ex);
                    }
                }

                await period.WaitTimeLeftAsync();
            }
        }

        public void Close()
        {
            if (IsOpen)
            {
                var status = _ftdi.Close();
                if (status == FT_STATUS.FT_OK)
                {
                    ConnectionClosed?.Invoke(this);
                }
                else
                {
                    var msg = $"Не удалось закрыть порт {PortName}";
                    var fullMsg = $"{msg}, статус: {status}, порт открыт: {IsOpen}";
                    Logger.LogError(msg, fullMsg);
                }

                if (!IsOpen)
                {
                    PortName = null;
                }
            }
        }

        FT_DEVICE_INFO_NODE _openedDevice;

        public void Open(string portName, int baudRate)
        {
            if (IsOpen)
            {
                Close();
            }

            var status = getDevice(out _openedDevice);
            status = status == FT_STATUS.FT_OK ? _ftdi.OpenByLocation(_openedDevice.LocId) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetBaudRate(baudRate.ToUInt32()) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetDataCharacteristics(8, 0, 0) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetFlowControl(0x0000, 0x00, 0x00) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetLatency(1) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetUSBParameters(32768, 0) : status;
            status = status == FT_STATUS.FT_OK ? _ftdi.SetTimeouts(1, 1) : status;

            if (status == FT_STATUS.FT_OK)
            {
                PortName = portName;

                ConnectionEstablished?.Invoke(this);
            }
            else
            {
                Logger.LogError($"Не удалось открыть порт {portName}", $"-MSG, статус: {status}");
                
                _openedDevice = null;
                Close();
            }

            FT_STATUS getDevice(out FT_DEVICE_INFO_NODE device)
            {
                var index = portName
                    .SkipWhile(char.IsLetter)
                    .Aggregate()
                    .ParseToUInt32Invariant();
                device = getAllDevices().ElementAtOrDefault((int)index);

                return device == null ? FT_STATUS.FT_DEVICE_NOT_FOUND : FT_STATUS.FT_OK;
            }
        }

        async Task updateAwailablePortsLoop(CancellationToken cancellation)
        {
            await ThreadingUtils.ContinueAtDedicatedThread(cancellation);

            var period = new PeriodDelay(1000);
            while (true)
            {
                try
                {
                    using (await Locker.AcquireAsync())
                    {
                        var devices = getAllDevices();
                        PortNames = devices.Length.Range().Select(i => PORT_PREFIX + i);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(null, $"Ошибка обновления списка устройств", ex);
                }

                await period.WaitTimeLeftAsync(cancellation);
            }
        }

        FT_DEVICE_INFO_NODE[] getAllDevices()
        {
            uint numOfDevices = 0;
            var status = _ftdi.GetNumberOfDevices(ref numOfDevices);
            var devices = new FT_DEVICE_INFO_NODE[numOfDevices];
            status = status == FT_STATUS.FT_OK ? _ftdi.GetDeviceList(devices) : status;

            return status == FT_STATUS.FT_OK ? devices : new FT_DEVICE_INFO_NODE[0];
        }
    }
}
