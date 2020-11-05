using System.Threading.Tasks;
using Utilities.Types;

namespace FlashDumpLoaderExports
{
    public interface IFlashDumpLoader
    {
        Task LoadAsync(string flashDumpPath, AsyncOperationInfo operationInfo);
    }
}
