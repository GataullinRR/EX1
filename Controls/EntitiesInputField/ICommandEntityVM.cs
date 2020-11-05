using DeviceBase.IOModels;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controls
{
    public interface ICommandEntityVM
    {
        EntityDescriptor Descriptor { get; }
        ValueVM<object> EntityValue { get; }
    }
}
