using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public interface ILoadDumpFeature
    {
        Task Load(string dumpPath, AsyncOperationInfo operationInfo);
    }
}
