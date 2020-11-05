namespace DataViewWidget
{
    abstract class ChartCommandBase : IChartCommand
    {
        protected ChartCommandBase(ChartCommand command)
        {
            Command = command;
        }

        public ChartCommand Command { get; }

        public override string ToString()
        {
            return Command.ToString();
        }
    }
}
