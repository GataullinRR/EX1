namespace DataViewWidget
{
    public enum ChartCommand
    {
        /// <summary>
        /// Contains referenses to datasources
        /// Removes everything, then sets titles, creates panes, etc...
        /// </summary>
        INITIALIZE,

        /// <summary>
        /// Gets visibilities from <see cref="InitializeCommand.CurveInfos"/>
        /// </summary>
        UPDATE_CURVES_VISIBILITY,
        /// <summary>
        /// Renders range of rows from <see cref="InitializeCommand.Rows"/>
        /// </summary>
        RENDER_AREA,
        /// <summary>
        /// Sets chart's settings (autoscale... etc)
        /// </summary>
        SET_VIEW_MODE,
        /// <summary>
        /// Removes all the points from chart (not from <see cref="InitializeCommand.Rows"/>)
        /// </summary>
        CLEAR,
    }

    public interface IChartCommand
    {
        ChartCommand Command { get; }
    }
}
