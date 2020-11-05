using Calibrators.ViewModels.Inclinometr;
using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vectors;
using Utilities;
using Utilities.Extensions;
using WPFUtilities.Extensions;
using OxyPlot.Wpf;
using System.Collections.Specialized;

namespace Calibrators.Views
{
    /// <summary>
    /// Interaction logic for ErrorGraph.xaml
    /// </summary>
    public partial class Chart : UserControl
    {
        internal static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(PlotVM[]), typeof(Chart), new PropertyMetadata(null, pointsChanged));

        internal PlotVM[] DataSource
        {
            get { return (PlotVM[])GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        public Chart()
        {
            InitializeComponent();
        }

        static void pointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ui = (Chart)d;

            ui.reloadPoints();
        }

        void reloadPoints()
        {
            ug_PlotsContainer.Children.Clear();
            if (DataSource != null)
            {
                var i = 0;
                foreach (var plot in DataSource)
                {
                    var xAxis = new LinearAxis()
                    {
                        Position = OxyPlot.Axes.AxisPosition.Bottom, 
                        Title = plot.XAxisName 
                    };
                    var yAxis = new LinearAxis()
                    {
                        Position = OxyPlot.Axes.AxisPosition.Left,
                        Title = plot.YAxisName,
                        AxisTickToLabelDistance = 5
                    };
                    xAxis.IsZoomEnabled = false;
                    xAxis.IsPanEnabled = false;
                    yAxis.IsZoomEnabled = false;
                    yAxis.IsPanEnabled = false;

                    var plotView = new Plot();
                    ug_PlotsContainer.Children.Add(plotView);

                    plotView.Title = plot.Name;
                    plotView.Axes.Add(xAxis);
                    plotView.Axes.Add(yAxis);
                    foreach (var curve in plot.Curves)
                    {
                        plotView.Series.Add(new LineSeries()
                        {
                            ItemsSource = curve.Points,
                            Color = curve.Color,
                            Title = curve.Name
                        });
                    }
                    i++;
                }
            }
        }
    }
}