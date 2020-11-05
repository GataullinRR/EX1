using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFUtilities.Types;

namespace InitializationExports
{
    public interface IDeviceInitializationVM : INotifyPropertyChanged
    {
        ActionCommand Initialize { get; }
        bool IsInitialized { get; }
    }
}
