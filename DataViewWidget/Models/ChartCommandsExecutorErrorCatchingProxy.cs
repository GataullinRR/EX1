using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataViewWidget
{
    class ChartCommandsExecutorErrorCatchingProxy : ChartCommandsExecutorProxyBase
    {
        public ChartCommandsExecutorErrorCatchingProxy(IChartCommandsExecutor @base) : base(@base)
        {

        }

        public override void ExecuteCommands(IList<IChartCommand> commands)
        {
            try
            {
                base.ExecuteCommands(commands);
            }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка обновления графика", ex);
            }
        }

        public override Task ExecuteCommandsAsync(IList<IChartCommand> commands)
        {
            try
            {
                return base.ExecuteCommandsAsync(commands);
            }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка обновления графика", ex);

                return Task.CompletedTask;
            }
        }
    }
}
