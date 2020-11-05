using DeviceBase.Devices;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandExports
{
    public interface ICommandHandlerWidgetsFactory
    {
        ICommandHandlerWidget TryInstantiateWidgetOrNull(IRUSDevice device, Command request, BusyObject busy);
    }
}
