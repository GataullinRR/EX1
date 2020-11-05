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

namespace Calibrators.Views
{
    /// <summary>
    /// Interaction logic for TunableNumber.xaml
    /// </summary>
    public partial class TunableNumber : UserControl
    {
        public double? Value
        {
            get { return (double?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public Interval? ValueRange
        {
            get { return (Interval?)GetValue(ValueRangeProperty); }
            set { SetValue(ValueRangeProperty, value); }
        }

        public double SmallStep
        {
            get { return (double)GetValue(SmallStepProperty); }
            set { SetValue(SmallStepProperty, value); }
        }

        public double BigStep
        {
            get { return (double)GetValue(BigStepProperty); }
            set { SetValue(BigStepProperty, value); }
        }

        public GridLength ValueWidth
        {
            get { return (GridLength)GetValue(ValueWidthProperty); }
            set { SetValue(ValueWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double?), typeof(TunableNumber), 
            new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for ValueRange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueRangeProperty =
            DependencyProperty.Register("ValueRange", typeof(Interval?), typeof(TunableNumber), 
            new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for SmallStep.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SmallStepProperty =
            DependencyProperty.Register("SmallStep", typeof(double), typeof(TunableNumber), 
            new PropertyMetadata(0D));

        // Using a DependencyProperty as the backing store for BigStep.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BigStepProperty =
            DependencyProperty.Register("BigStep", typeof(double), typeof(TunableNumber), 
            new PropertyMetadata(0D));

        // Using a DependencyProperty as the backing store for ValueWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueWidthProperty =
            DependencyProperty.Register("ValueWidth", typeof(GridLength), typeof(TunableNumber),
            new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

        public TunableNumber()
        {
            InitializeComponent();
        }

        void AddBig(object sender, RoutedEventArgs e)
        {
            updateValue(BigStep);
        }

        void AddSmall(object sender, RoutedEventArgs e)
        {
            updateValue(SmallStep);
        }

        void SubSmall(object sender, RoutedEventArgs e)
        {
            updateValue(-SmallStep);
        }

        void SubBig(object sender, RoutedEventArgs e)
        {
            updateValue(-BigStep);
        }

        void updateValue(double step)
        {
            var newValue = Value + step;
            bool isInRange = newValue.HasValue
                ? ValueRange?.Contains(newValue.Value) ?? true
                : false;
            Value = isInRange
                ? newValue
                : Value;
        }
    }
}
