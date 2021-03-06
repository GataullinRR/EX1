﻿using Common;
using DeviceBase.Models;
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
using Utilities.Types;
using Utilities.Extensions;
using System.ComponentModel;
using MVVMUtilities.Types;
using System.Collections.Specialized;
using WPFUtilities.Extensions;
using DataViewExports;
using Utilities;
using WidgetHelpers;

namespace DataViewWidget
{
    /// <summary>
    /// Interaction logic for MainDataWidget.xaml
    /// </summary>
    public partial class DataView : UserControl, IWidget, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public WidgetIdentity FunctionId { get; private set; }
        public Control View => this;
        public WidgetType Type => WidgetType.DATA;
        public DataViewVM Model { get; private set; }

        DataView()
        {
            InitializeComponent();

            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid));
            dpd.AddValueChanged(dg_PacketDTO, DataView_ItemsSourceChanged);
        }

        public static IEnumerator<ResolutionStepResult> InstantiationCoroutine(string name, object activationScope, IDIContainer container)
        {
            return Helpers.Coroutine(instantiate);

            void instantiate()
            {
                var widget = new DataView()
                {
                    Model = new DataViewVM(
                        container.ResolveSingle<IDataStorageVM>(activationScope)),
                    FunctionId = new WidgetIdentity(name,
                        container.ResolveSingle<string>(),
                        activationScope)
                };

                container.Register<IGraphicViewSetingVM>(widget.Model.GraphicVM.Settings, activationScope);
                container.Register<IWidget>(widget);
            }
        }

        void DataView_ItemsSourceChanged(object sender, EventArgs e)
        {
            if (dg_PacketDTO.ItemsSource != null)
            {
#warning multiple subscribing possible
                hookToCollectionChanged();
            }
        }

        void DataView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) // Collection was cleared
            {
                unhookFromCollectionChanged();

                // To regenerate autogenerated columns...
                var isBinding = dg_PacketDTO.GetBindingExpression(DataGrid.ItemsSourceProperty).ParentBinding;
                dg_PacketDTO.ItemsSource = null;
                dg_PacketDTO.SetBinding(DataGrid.ItemsSourceProperty, isBinding);

                hookToCollectionChanged();
            }
        }
        void hookToCollectionChanged()
        {
            ((ISlimEnhancedObservableCollection<object>)dg_PacketDTO.ItemsSource).CollectionChanged += DataView_CollectionChanged;
        }
        void unhookFromCollectionChanged()
        {
            ((ISlimEnhancedObservableCollection<object>)dg_PacketDTO.ItemsSource).CollectionChanged -= DataView_CollectionChanged;
        }

        void Cb_Draw_Loaded(object sender, RoutedEventArgs e)
        {
            var checkBox = dg_PacketDTO.FindVisualChildrenByName<CheckBox>("cb_Draw")
                .Skip(1)
                .Find((CheckBox)sender);
            if (checkBox.Found)
            {
                var toShownBinding = new Binding($"{nameof(Model.GraphicVM)}.{nameof(Model.GraphicVM.SourceVM)}.{nameof(Model.GraphicVM.SourceVM.PointsSource)}.{nameof(Model.GraphicVM.SourceVM.PointsSource.CurveInfos)}[{checkBox.Index}].{nameof(ICurveInfo.IsShown)}");
                toShownBinding.Source = Model;
                toShownBinding.Mode = BindingMode.TwoWay;
                checkBox.Value.SetBinding(CheckBox.IsCheckedProperty, toShownBinding);
            }
        }

        void Dg_PacketDTO_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var header = e.Column.Header.To<string>();
            if (header == "DateTime")
            {
                e.Cancel = true;
            }
#warning duplicate constant
            else if (header.Contains("*")) // See: DataPacketDTOBuilder.SAME_NAME_ESCAPING
            {
                e.Column.Header = header.Remove("*");
            }
        }

        void Dg_PacketDTO_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = ((dynamic)e.Row.DataContext).DateTime;
        }
    }
}
