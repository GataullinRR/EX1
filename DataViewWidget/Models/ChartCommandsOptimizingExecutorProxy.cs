using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Extensions;
using Common;

namespace DataViewWidget
{
    /// <summary>
    /// Optimazes chart commands execution by buffering and removing meaningles command (e.g. <see cref="RenderAreaCommand"/> is meaningles before <see cref="InitializeCommand"/>)
    /// </summary>
    class ChartCommandsOptimizingExecutorProxy : ChartCommandsExecutorProxyBase
    {
        readonly List<IChartCommand> _commandsQueue = new List<IChartCommand>();

        public ChartCommandsOptimizingExecutorProxy(int executionLoopDelay, IChartCommandsExecutor @base) : base(@base)
        {
            updateLoop();

            return; /////////////////////////////

            async void updateLoop()
            {
                // Simple logic at the moment
                while (true)
                {
                    await Task.Delay(executionLoopDelay);

                    if (_commandsQueue.Count > 0)
                    {
                        optimizeCommandsSequence();
                        // Because ui thread can change the list while awaiting
                        var commands = _commandsQueue.ToArray();
                        try
                        {
                            await base.ExecuteCommandsAsync(commands);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorEverywhere("Ошибка обновления графика", ex);
                            _commandsQueue.Clear();
                        }
                        finally
                        {
                            foreach (var command in commands)
                            {
                                _commandsQueue.Remove(command);
                            }
                        }
                    }

                    void optimizeCommandsSequence()
                    {
                        for (int i = 0; i < _commandsQueue.Count; i++)
                        {
                            var command = _commandsQueue[i];
                            switch (command.Command)
                            {
                                case ChartCommand.INITIALIZE:
                                    _commandsQueue.Set(null, 0, i);
                                    break;
                                case ChartCommand.CLEAR:
                                case ChartCommand.RENDER_AREA:
                                    for (int k = 0; k < i; k++)
                                    {
                                        var c = _commandsQueue[k];
                                        if (c != null && (c.Command == ChartCommand.RENDER_AREA || c.Command == ChartCommand.CLEAR))
                                        {
                                            _commandsQueue[k] = null;
                                        }
                                    }
                                    break;
                            }
                        }
                        _commandsQueue.RemoveAll(c => c == null);
                    }
                }
            }
        }

        public override void ExecuteCommands(IList<IChartCommand> commands)
        {
            _commandsQueue.AddRange(commands);
        }

        public override Task ExecuteCommandsAsync(IList<IChartCommand> commands)
        {
            ExecuteCommands(commands);

            return Task.CompletedTask;
        }
    }
}
