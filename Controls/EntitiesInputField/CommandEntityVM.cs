using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System.ComponentModel;
using Utilities;

namespace Controls
{
    public class CommandEntityVM : ICommandEntityVM, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public EntityDescriptor Descriptor { get; }
        public ValueVM<object> EntityValue { get; }
        public IDataEntity Entity => Descriptor.InstantiateEntity(Descriptor.Serialize(EntityValue.ModelValue));

        public CommandEntityVM(EntityDescriptor descriptor)
        {
            Descriptor = descriptor;

            EntityValue = new ValueVM<object>(
                Descriptor.DeserializeFromString,
                Descriptor.SerializeToString,
                (v) => CommonUtils.TryOrDefault(() => Descriptor.ValidateValueRange(v)));
            EntityValue.ModelValue = descriptor.DefaultEntity.Value;
        }
    }
}
