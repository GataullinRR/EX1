using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataViewWidget
{
    class ChartCommandsExecutorProxyBase : IChartCommandsExecutor
    {
        readonly IChartCommandsExecutor _base;

        public ChartCommandsExecutorProxyBase(IChartCommandsExecutor @base)
        {
            _base = @base;
        }

        public virtual void ExecuteCommands(IList<IChartCommand> commands)
        {
            _base.ExecuteCommands(commands);
        }

        public virtual Task ExecuteCommandsAsync(IList<IChartCommand> commands)
        {
            return _base.ExecuteCommandsAsync(commands);
        }
    }
}
