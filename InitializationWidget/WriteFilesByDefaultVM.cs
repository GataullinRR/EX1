using System;
using System.Threading.Tasks;
using MVVMUtilities.Types;
using DeviceBase;
using System.Linq;
using Utilities.Extensions;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.Devices;
using Common;
using WPFUtilities.Types;
using Vectors;
using System.Threading;

namespace InitializationWidget
{
    public class WriteFilesByDefaultVM
    {
        public event EventHandler SuccessfullyWritten;

        public BusyObject IsBusy { get; }
        public DoubleValueVM SerialNumber { get; } = new DoubleValueVM(new Interval(0, 65534), true, 0);
        public ValueVM<string> Modification { get; } = new ValueVM<string>(
            v => v,
            v => v,
            v => v == null ? false : v.Length == 2 && v.IsASCII());
        public ActionCommand WriteAllFilesByDefault { get; }

        public WriteFilesByDefaultVM(IRUSDevice device, BusyObject isBusy)
        {
            IsBusy = isBusy;
            WriteAllFilesByDefault = new ActionCommand(writeDefaultFiles, isBusy);
            SerialNumber.ModelValue = 1;
            Modification.ModelValue = "??";

            async Task writeDefaultFiles()
            {
                using (isBusy.BusyMode)
                {
                    SerialNumber.AssertValue();
                    Modification.AssertValue();
                    
                    if (!UserInteracting.RequestAcknowledgement("Запись файлов по умолчанию", "Данная операция перезапишет все файлы файлами по умолчанию-NL-NLПродолжить?"))
                    {
                        return;
                    }

                    var hadError = false;
                    var date = DateTime.UtcNow;
                    foreach (var file in Files.Descriptors.Where(d => d.Key.TargetDeviceId == device.Id))
                    {
                        var entities = file.Value.Descriptors.Select(d => d.FileDefaultDataEntity);
                        entities = Files.SetBurnDate(entities, date);
                        entities = Files.SetSerialNumber(entities, SerialNumber.ModelValue.ToInt32());
                        entities = Files.SetFileEntity(entities, FileEntityType.MODIFICATION, Modification.ModelValue);
                        var request = file.Key.FileType.GetRequestAddress();

                        var result = await device.BurnAsync(request, entities, DeviceOperationScope.DEFAULT, CancellationToken.None);
                        if (result.Status != BurnStatus.OK)
                        {
                            Logger.LogErrorEverywhere("Ошибка операции");
                            hadError = true;

                            break;
                        }
                    }

                    if (!hadError)
                    {
                        SuccessfullyWritten?.Invoke(this);
                    }
                }
            }
        }
    }
}
