using DeviceBase.Models;
using MVVMUtilities.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities.Extensions;
using Utilities.Types;
using Vectors;
using ZedGraph;

namespace DataViewWidget
{
#warning refactoring needed
    /// <summary>
    /// Thanks to Munasipov for the idea of updating chart after set time intervals.
    /// The class binds to viewmodel events and sends command to <see cref="IChartCommandsExecutor"/>
    /// </summary>
    public partial class Graphic : System.Windows.Controls.UserControl
    {
        readonly IChartCommandsExecutor _executor;
        readonly ZedGraphControl _chart;
        int _yPosition = 0;
        int _manualScrollYPosition = 0;

        int actualScrollYPosition
        {
            get => sb_Scroll.Value.ToInt32();
            set => sb_Scroll.Value = value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(GraphicVM), typeof(Graphic),
            new PropertyMetadata(null, viewModelChanged));
        public GraphicVM ViewModel
        {
            get { return (GraphicVM)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public Graphic()
        {
            InitializeComponent();

            _chart = (ZedGraphControl)_host.Child;
            {
                _chart.ContextMenuBuilder += _graphic_ContextMenuBuilder;
                _chart.IsEnableVPan = false;
                _chart.IsEnableHZoom = false;
                _chart.IsEnableVZoom = false;
                _chart.IsSynchronizeYAxes = true;
                _chart.IsShowVScrollBar = false;
                _chart.IsAutoScrollRange = false;
                _chart.MasterPane.InnerPaneGap = 0;
                _chart.MasterPane.Title.IsVisible = false;
                _chart.GraphPane.Border.IsVisible = false;
                _chart.GraphPane.Legend.IsVisible = false;
                _chart.GraphPane.Title.IsVisible = false;
                _chart.VerticalScroll.Enabled = false;
                _chart.VerticalScroll.Visible = false;
                _chart.IsShowVScrollBar = false;
                _yPosition = 0;

                sb_Scroll.Visibility = Visibility.Hidden;
                sb_Scroll.SmallChange = 1;
                sb_Scroll.Scroll += Sb_Scroll_Scroll;
            }

            _executor = ChartModelsFactory.CreateZedGraphChartCommandExecutor(_chart);
            _executor = ChartModelsFactory.CreateZedGraphChartCommandExecutor(_chart);

            void _graphic_ContextMenuBuilder(
                ZedGraphControl sender,
                ContextMenuStrip menuStrip,
                System.Drawing.Point mousePt,
                ZedGraphControl.ContextMenuObjectState objState)
            {
                for (int i = 0; i < menuStrip.Items.Count; i++)
                {
                    var item = menuStrip.Items[i];
                    Debug.Print(item.Tag.ToString());

                    if (((string)item.Tag).IsOneOf("set_default", "undo_all", "unzoom"))
                    {
                        menuStrip.Items.Remove(item);
                        Debug.Print("Deleted");
                    }
                }
            }
        }

        static void viewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                throw new NotSupportedException();
            }

            var g = d.To<Graphic>();
            if (e.NewValue != null)
            {
                g.hookEvents();
                g.rerenderEverything();
            }
        }

        void hookEvents()
        {
            if (ViewModel.SourceVM != null)
            {
                hookPointsSourceEvents();
                ViewModel.SourceVM.PropertyChanged += reinitializeChart;
#warning multiple subscribing!
                ViewModel.SourceVM.PropertyChanged += (o, e) => hookPointsSourceEvents();
                ViewModel.Settings.PropertyChanged += setingsChanged;
            }

            return; ///////////////////

            void reinitializeChart(object sender, EventArgs e)
            {
                sendInitializationCommand();
                updateCurveGroupShow();
                loadYAxis(); 
            }

            void hookPointsSourceEvents()
            {
                ViewModel.SourceVM.PointsSource.CurveInfos.CollectionChanged += reinitializeChart;
                ViewModel.SourceVM.PointsSource.CurveInfos.ItemPropertyChanged += updateCurvesVisibilityState;
                ViewModel.SourceVM.PointsSource.PointsRows.CollectionChanged += renderPoints;
            }

            void updateCurvesVisibilityState(object s, EventArgs e)
            {
                updateCurveGroupShow();
                updateCurvesVisibility();
            }

            void updateCurveGroupShow()
            {
                ViewModel.Settings.CurveGroupShow = ViewModel.SourceVM.PointsSource.CurveInfos.Select(ci => ci.IsShown).Distinct().Count() > 1
                    ? (bool?)null
                    : ViewModel.SourceVM.PointsSource.CurveInfos.FirstElementOrDefault()?.IsShown ?? ViewModel.Settings.CurveGroupShow;
            }

            void setingsChanged(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(ViewModel.Settings.YAxisLength):
                        setYAxisLength(ViewModel.Settings.YAxisLength);
                        updateScrollState();
                        break;

                    case nameof(ViewModel.Settings.CurveGroupShow):
                        if (ViewModel.Settings.CurveGroupShow != null)
                        {
                            using (ViewModel.SourceVM.PointsSource.CurveInfos.ItemChangesEventSuppressingModeHolder)
                            {
                                ViewModel.SourceVM.PointsSource.CurveInfos.ForEach(ci => ci.IsShown = ViewModel.Settings.CurveGroupShow.Value);
                            }
                            updateCurvesVisibility();
                        }
                        break;

                    case nameof(ViewModel.Settings.IsAutoscaleEnabled):
                        setMode();
                        break;

                    case nameof(ViewModel.Settings.IsAutoscrollEnabled):
                        updateScrollingMode();
                        break;

                    default:
                        break;
                }
            }
        }

        void rerenderEverything()
        {
            _yPosition = ViewModel.SourceVM.PointsSource?.PointsRows?.Count ?? 0;
            actualScrollYPosition = 0;
            var range = IntInterval.Zero;
            if (ViewModel.Settings.IsAutoscrollEnabled)
            {
                range = IntInterval.ByLenAndEnd(
                    ViewModel.SourceVM.PointsSource.PointsRows.Count, 
                    ViewModel.Settings.YAxisLength);
            }
            else
            {
                range = IntInterval.ByLen(
                    actualScrollYPosition,
                    ViewModel.Settings.YAxisLength);
            }
            sendInitializationCommand();
            showData(range.From, range.Len);
        }
        void renderPoints(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _yPosition++;
                    updateScrollState();
                    var range = IntInterval.Zero;
                    if (ViewModel.Settings.IsAutoscrollEnabled)
                    {
                        range = IntInterval.ByLenAndEnd(
                            ViewModel.SourceVM.PointsSource.PointsRows.Count,
                            ViewModel.Settings.YAxisLength);
                    }
                    else
                    {
                        range = IntInterval.ByLen(
                            actualScrollYPosition,
                            ViewModel.Settings.YAxisLength);
                    }
                    showData(range.From, range.Len);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    clear();
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        void Sb_Scroll_Scroll(object sender, EventArgs e)
        {
            if (!ViewModel.Settings.IsAutoscrollEnabled)
            {
                var range = IntInterval.ByLen(actualScrollYPosition, ViewModel.Settings.YAxisLength);

                showData(range.From, range.Len);
            }
        }

        void updateScrollingMode()
        {
            if (ViewModel.Settings.IsAutoscrollEnabled)
            {
                _manualScrollYPosition = actualScrollYPosition;
            }
            else
            {
                actualScrollYPosition = _manualScrollYPosition;
            }

            loadYAxis();
        }

        void loadYAxis()
        {
            updateScrollState();

            var yMin = 0;
            var yMax = 0;
            if (ViewModel.Settings.IsAutoscrollEnabled)
            {
                yMin = (_yPosition - ViewModel.Settings.YAxisLength).NegativeToZero();
                yMax = yMin + ViewModel.Settings.YAxisLength;
            }
            else
            {
                yMin = actualScrollYPosition;
                yMax = yMin + ViewModel.Settings.YAxisLength;
            }

            showData(yMin, ViewModel.Settings.YAxisLength);
        }
        void updateScrollState()
        {
            sb_Scroll.IsEnabled = !ViewModel.Settings.IsAutoscrollEnabled;
            sb_Scroll.Visibility = ViewModel.Settings.IsAutoscrollEnabled
                ? Visibility.Hidden
                : Visibility.Visible;
            sb_Scroll.Maximum = (ViewModel.SourceVM.PointsSource.PointsRows.Count - ViewModel.Settings.YAxisLength).NegativeToZero();
        }

        void setYAxisLength(int newLen)
        {
            if (ViewModel.Settings.IsAutoscrollEnabled)
            {
                showData(_yPosition - newLen, newLen);
            }
            else
            {
                showData(actualScrollYPosition, newLen);
            }
        }

        /// <summary>
        /// Adds points which lie from <paramref name="position"/> to <paramref name="position"/> + <paramref name="len"/>, does not update <see cref="_yPosition"/>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="len"></param>
        void showData(int position, int len)
        {
            var range = IntInterval.ByLen(position, len);
            
            _executor.ExecuteCommands(new[] { new RenderAreaCommand(range) });
        }
        void updateCurvesVisibility()
        {
            _executor.ExecuteCommands(new[] { new UpdateCurvesVisibilityCommand() });
        }
        void setMode()
        {
            _executor.ExecuteCommands(new[] { new SetViewModeCommand(ViewModel.Settings.IsAutoscaleEnabled) });
        }
        void clear()
        {
            _executor.ExecuteCommands(new[] { new ClearCommand() });
        }
        void sendInitializationCommand()
        {
            _executor.ExecuteCommands(new IChartCommand[]
            {
                ChartModelsFactory.CreateInitializationCommand(ViewModel.SourceVM.PointsSource)
            });
        }
    }
}
