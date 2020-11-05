using System;
using System.Collections.Generic;
using MVVMUtilities.Types;
using DeviceBase;
using System.Linq;
using Utilities.Extensions;
using DeviceBase.IOModels;
using DeviceBase.Devices;
using Calibrators;
using Common;
using WPFUtilities.Types;
using DeviceBase.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DataViewExports;
using RUSTelemetryStreamSenderExports;

namespace DataRequestWidget
{
    /// <summary>
    /// Provides access to all the data
    /// </summary>
    public class DeviceDataVM : IDataStorageVM
    {
        static readonly DataPacketDTOBuilder _dtoBuilder = new DataPacketDTOBuilder();

        readonly IRUSDevice _device;
        IEnumerable<ViewDataEntity> _lastEntities;
        ICurveInfo[] _curveInfosBeforeLastClear;

        public event PropertyChangedEventHandler PropertyChanged;

        public ActionCommand Clear { get; }
        public EnhancedObservableCollection<object> DTOs { get; }
            = new EnhancedObservableCollection<object>(new CollectionOptions(false));
        public IDataPointsStorage PointsSource { get; } = new DataPointsSource(
            new EnhancedObservableCollection<ICurveInfo>(), 
            new EnhancedObservableCollection<IPointsRow>(new CollectionOptions(false)));

        public DeviceDataVM(IRUSDevice device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            Clear = new ActionCommand((Action)clearAll);

            void clearAll()
            {
                _curveInfosBeforeLastClear = PointsSource.CurveInfos.ToArray();
                DTOs.Clear();
                PointsSource.PointsRows.Clear();
                PointsSource.CurveInfos.Clear();
                _lastEntities = null;
            }
        }

        public void Update(IEnumerable<ViewDataEntity> entities, RowDecoration decoration = default)
        {
            if (isFormatChanged())
            {
                Logger.LogOKEverywhere("Формат данных обновлен");

                Clear.Execute();
            }

            entities = entities.ToArray();
            var columns = 
                ("DateTime", DateTime.Now.ToString("HH:mm:ss")).ToSequence()
                .Concat(entities.Select(e => (e.CurveName, e.GetAsString(2))));
            var dto = _dtoBuilder.Instantiate(columns);
            DTOs.Insert(0, dto);
            if (DTOs.Count > 1000)
            {
                DTOs.RemoveLast();
            }
            var points = entities
                .Select(e => e.PointValue)
                .SkipNulls()
                .Select(p => p.Value)
                .ToArray();
            PointsSource.PointsRows.Add(new DecoratedPointsRow(points, decoration));
            if (PointsSource.CurveInfos.Count == 0)
            {
                PointsSource.CurveInfos.AddRange(entities
                    .Where(e => e.PointValue.HasValue)
                    .Select(e => new CurveInfo(e.CurveName, _curveInfosBeforeLastClear?.FirstOrDefault(ci => ci.Title == e.CurveName)?.IsShown ?? true)));
            }

            _lastEntities = entities;

            bool isFormatChanged()
            {
                if (_lastEntities == null)
                {
                    return false;
                }
                else
                {
                    var isSame = _lastEntities.Count() == entities.Count()
                              && _lastEntities.Zip(entities, (p1, p2) => p1.IsSameAs(p2)).AllTrue();

                    return !isSame;
                }
            }
        }
    }
}
