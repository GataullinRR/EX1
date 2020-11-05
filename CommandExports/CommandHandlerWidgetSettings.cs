namespace CommandExports
{
    public class CommandHandlerWidgetSettings
    {
        public bool ShowProgress { get; }
        public bool AllowCancelling { get; }

        public CommandHandlerWidgetSettings(bool showProgress, bool allowCancelling)
        {
            ShowProgress = showProgress;
            AllowCancelling = allowCancelling;
        }
    }
}
