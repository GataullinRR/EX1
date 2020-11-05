using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RUSManagingToolExports
{
    public interface IDeviceHandler
    {
        Task OnDisconnectedAsync();
        Task OnConnectedAsync();
    }
}
