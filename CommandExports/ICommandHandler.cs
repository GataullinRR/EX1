using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace CommandExports
{
    public interface ICommandHandlerModel
    {
        Task OnReadAsync(AsyncOperationInfo operationInfo);
        Task OnBurnAsync(AsyncOperationInfo operationInfo);
    }
}
