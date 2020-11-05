using Common;
using DeviceBase.Models;
using RUSTelemetryStreamSenderExports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;
using ZedGraph;

namespace DataViewWidget
{
    class ZedGraphChartCommandExecutor : IChartCommandsExecutor
    {
        /// <summary>
        /// Holds panes, manages theirs visibilities
        /// </summary>
        class PanesStorage : IEnumerable<GraphPane>
        {
            readonly ZedGraphControl _graphic;
            readonly List<GraphPane> _allPanes = new List<GraphPane>();
            /// <summary>
            /// Applies when not in single pane mode
            /// </summary>
            IEnumerable<(int PaneI, bool IsVisible)> _visibilities = new (int PaneI, bool IsVisible)[] { };

            public int Count => _allPanes.Count;

            public GraphPane this[int index]
            {
                get
                {
                    return _allPanes[index];
                }
            }

            public PanesStorage(ZedGraphControl graphic)
            {
                _graphic = graphic;

                _graphic.VerticalScroll.Enabled = false;
                _graphic.VerticalScroll.Visible = false;
            }

            public void Reset()
            {
                _allPanes.ForEach(p => p.CurveList.Clear());
                _allPanes.Clear();
                _graphic.MasterPane.PaneList.Clear();
            }

            public void RegisterPanes(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var pane = new GraphPane();
                    pane.XAxis.Title.IsVisible = false;
                    pane.YAxis.Title.IsVisible = false;
                    pane.YAxis.Scale.IsReverse = true;
                    pane.Border.IsVisible = false;
                    pane.Legend.IsVisible = false;
                    pane.YAxis.IsVisible = false;
                    pane.Margin = new Margin() { All = 0 };
                    pane.IsFontsScaled = false;
                    pane.XAxis.Scale.MagAuto = false;

                    _allPanes.Add(pane);
                }

                _graphic.MasterPane.PaneList.Clear();
                _graphic.MasterPane.PaneList.AddRange(_allPanes); // Just adding in the loop doesnt work correctly. Dont know why.
            }

            public void SetVisibilities(IEnumerable<(int PaneI, bool IsVisible)> panesVisibility)
            {
                _visibilities = panesVisibility;

                foreach (var kvp in panesVisibility)
                {
                    setVisibility(kvp.PaneI, kvp.IsVisible);
                }

                void setVisibility(int paneI, bool isVisible)
                {
                    var pane = this[paneI];
                    if (isVisible)
                    {
                        if (!_graphic.MasterPane.PaneList.Contains(pane))
                        {
                            // The idea is to find the first left neighbor to given pannel which exist in 
                            // _graphic.MasterPane.PaneList and place panel right after it
                            var insertionIndex = 0;
                            foreach (var shownPaneI in _graphic.MasterPane.PaneList.Count.Range().Reverse())
                            {
                                foreach (var p in this.Take(paneI).Reverse())
                                {
                                    if (_graphic.MasterPane[shownPaneI] == p)
                                    {
                                        insertionIndex = shownPaneI + 1;
                                        break;
                                    }
                                }

                                if (insertionIndex != 0)
                                {
                                    break;
                                }
                            }

                            _graphic.MasterPane.PaneList.Insert(insertionIndex, pane);
                        }
                    }
                    else
                    {
                        _graphic.MasterPane.PaneList.Remove(pane);
                    }
                }
            }

            public IEnumerator<GraphPane> GetEnumerator()
            {
                return _allPanes.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _allPanes.GetEnumerator();
            }
        }

        readonly ZedGraphControl _chart;
        readonly PanesStorage _panes;
        InitializeCommand _dataSource;

        public ZedGraphChartCommandExecutor(ZedGraphControl chart)
        {
            _chart = chart;
            _panes = new PanesStorage(_chart);
        }

        public void ExecuteCommands(IList<IChartCommand> commands)
        {
            throw new NotSupportedException("Use Async");
        }
        public async Task ExecuteCommandsAsync(IList<IChartCommand> commands)
        {
            using (Logger.Indent)
            {
                var sw = Stopwatch.StartNew();

                await executeCommandsAsync();
                render();

                Logger.LogOK(null, $"Выполнение команд отрисовки завершено за {sw.Elapsed.TotalMilliseconds:F2} мс");
            }

            async Task executeCommandsAsync()
            {
                Logger.LogInfo(null, "Выполнение команд графика...");

                foreach (var command in commands)
                {
                    Logger.LogInfo(null, $"Выполнение команды: {command.ToString()}");
                    await handleCommandAsync((dynamic)command);
                }
            }

            void render()
            {
                Logger.LogInfo(null, $"Отрисовка графика...");

                _chart.AxisChange();
                using (Graphics g = _chart.Parent.CreateGraphics())
                {
                    _chart.MasterPane.SetLayout(g, PaneLayout.SingleRow);
                }
                _chart.Invalidate();
            }
        }

        #region ### Command handlers ###

        Task handleCommandAsync(InitializeCommand initialize)
        {
            _dataSource = initialize;

            _panes.Reset();
            _panes.RegisterPanes(_dataSource.CurveInfos.Count);
            for (int i = 0; i < _panes.Count; i++)
            {
                var pane = _panes[i];
                pane.AddCurve(null, null, Color.Black, SymbolType.None);
                pane.Title.Text = _dataSource.CurveInfos.Count > i
                    ? _dataSource.CurveInfos[i].Title
                    : "NOT_SET";
            }
            setVisibility();

            return Task.CompletedTask;
        }
        async Task handleCommandAsync(RenderAreaCommand renderArea)
        {
            await renderRangeAsync(renderArea.AreaRange.From, renderArea.AreaRange.Len);
        }
        Task handleCommandAsync(UpdateCurvesVisibilityCommand points)
        {
            setVisibility();
            
            return Task.CompletedTask;
        }
        Task handleCommandAsync(ClearCommand clear)
        {
            foreach (var pane in _panes)
            {
                pane.CurveList.Clear();
            }

            return Task.CompletedTask;
        }
        Task handleCommandAsync(SetViewModeCommand viewMode)
        {
            foreach (var pane in _panes)
            {
                pane.XAxis.Scale.MaxAuto = viewMode.IsAutoscaleEnabled;
                pane.XAxis.Scale.MinAuto = viewMode.IsAutoscaleEnabled;
                _chart.IsEnableHZoom = !viewMode.IsAutoscaleEnabled;
                _chart.IsEnableHPan = !viewMode.IsAutoscaleEnabled;
            }

            return Task.CompletedTask;
        }
        /// <summary>
        /// Fallback
        /// </summary>
        /// <param name="command"></param>
        Task handleCommandAsync(IChartCommand command)
        {
            throw new NotSupportedException($"The command {command} is not supported!");
        }

        #endregion

        void setVisibility()
        {
            var visibilities = _dataSource.CurveInfos.Count
                .Range()
                .Zip(_dataSource.CurveInfos, (i, ci) => (i, ci.IsShown));
            _panes.SetVisibilities(visibilities);
        }

        /// <summary>
        /// Fills <see cref="_chart"/> with points lying from <paramref name="position"/> to <paramref name="position"/> + <paramref name="len"/>. Does not update <see cref="_yPosition"/>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="len"></param>
        async Task renderRangeAsync(int position, int len)
        {
            position = position.NegativeToZero();
            var numOfPoints = Math.Min(_dataSource.Rows.Source.RowsCount - position, len).NegativeToZero();
            var rows = await _dataSource.Rows.GetDecimatedRangeAsync(position, numOfPoints, new AsyncOperationInfo());
            var stepSize = numOfPoints / (double)rows.Length;
            var points = getPoints();
            for (int curveIndex = 0; curveIndex < _panes.Count; curveIndex++)
            {
                var pane = _panes[curveIndex];
                clearPane(pane);
                pane.YAxis.Scale.Min = position;
                pane.YAxis.Scale.Max = position + len - 1;

                for (int i = 0; i < rows.Length; i++)
                {
                    addPoint(pane, rows[i], points[curveIndex][i]);
                }
            }

            return; ////////////////////////////

            PointPair[][] getPoints()
            {
                var result = new PointPair[_panes.Count][];
                _panes.Count.Range()
                    .ToArray()
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                    .ForEach(curveIndex =>
                    {
                        result[curveIndex] = new PointPair[rows.Length];
                        for (int i = 0; i < rows.Length; i++)
                        {
                            // Bottleneck (10%)
                            result[curveIndex][i] = new PointPair()
                            {
                                X = rows[i].Points[curveIndex],
                                Y = (i * stepSize).Round() + position
                            };
                        }
                    })
                    .ToArray();

                return result;
            }

            void addPoint(GraphPane pane, IPointsRow row, PointPair point)
            {
                var curve = pane.CurveList[0];
                curve.AddPoint(point); // Bottleneck (9%)

                if (row is DecoratedPointsRow decoratedPointsRow)
                {
                    switch (decoratedPointsRow.Decoration)
                    {
                        case RowDecoration.NONE:
                            break;
                        case RowDecoration.LINE:
                            drawHorizontalLine();
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    void drawHorizontalLine()
                    {
                        var threshHoldLine = new LineObj(
                            Color.LightGray,
                            0,
                            point.Y,
                            1,
                            point.Y);
                        threshHoldLine.Location.CoordinateFrame = CoordType.XChartFractionYScale;
                        threshHoldLine.IsClippedToChartRect = true;
                        pane.GraphObjList.Add(threshHoldLine);
                    }
                }
            }
            void clearPane(GraphPane pane)
            {
                if (pane.CurveList.Count != 0)
                {
                    pane.CurveList[0].Clear();
                }
                pane.GraphObjList.Clear();
            }
        }
    }
}
