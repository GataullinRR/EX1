using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataViewWidget
{
    interface IChartCommandsExecutor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commands"></param>
        /// <exception cref="NotSupportedException">Command is not supported</exception>
        void ExecuteCommands(IList<IChartCommand> commands);
        Task ExecuteCommandsAsync(IList<IChartCommand> commands);
    }
}
