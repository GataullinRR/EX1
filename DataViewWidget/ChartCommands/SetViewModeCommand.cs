namespace DataViewWidget
{
    class SetViewModeCommand : ChartCommandBase
    {
        public SetViewModeCommand(bool isAutoscaleEnabled) : base(ChartCommand.SET_VIEW_MODE)
        {
            IsAutoscaleEnabled = isAutoscaleEnabled;
        }

        public bool IsAutoscaleEnabled { get; }

        public override string ToString()
        {
            return base.ToString() + $" (IsAutoscaleEnabled: {IsAutoscaleEnabled})";
        }
    }
}
