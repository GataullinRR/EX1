using DataViewExports;
using System;
using System.Text;
using ZedGraph;

namespace DataViewWidget
{
    static class ChartModelsFactory
    {
        const int MAX_AMOUNT_OF_ROWS_ON_CHART = 2000;
        const int CHART_EXECUTION_LOOP_DELAY = 100; 

        public static IChartCommandsExecutor CreateZedGraphChartCommandExecutor(ZedGraphControl chart)
        {
            IChartCommandsExecutor executor = new ZedGraphChartCommandExecutor(chart);
            executor = new ChartCommandsOptimizingExecutorProxy(CHART_EXECUTION_LOOP_DELAY, executor);
            executor = new ChartCommandsExecutorErrorCatchingProxy(executor);
            
            return executor;
        }

        public static IChartCommand CreateInitializationCommand(IDataPointsStorage storage)
        {
            var stream = storage.PointsRows as IRowsReaderProvider
                ?? new ListToRowsReaderProviderAdapter(storage.PointsRows);
            return new InitializeCommand(
                storage.CurveInfos,
                new DecimatedRowsReader(stream.RowsReader, MAX_AMOUNT_OF_ROWS_ON_CHART));
        }
    }
}
