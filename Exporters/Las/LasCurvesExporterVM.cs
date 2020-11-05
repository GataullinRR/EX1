using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using MVVMUtilities.Types;
using System.IO;
using WPFUtilities.Types;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DataViewExports;
using ExportersExports;

namespace Exporters.Las
{
    public class LasCurvesExporterVM : ICurvesExporterVM
    {
        readonly IPointsStorageProvider _deviceData;

        public string FormatName => "LAS";
        public ActionCommand Export { get; }

        public LasCurvesExporterVM(IPointsStorageProvider deviceData)
        {
            _deviceData = deviceData;

            Export = new ActionCommand(() => exportToLas(), () => hasCurves(), _deviceData);
            update();

            // hack
            async void update()
            {
                while (true)
                {
                    await Task.Delay(1000);
                    Export.Update();
                }
            }

            bool hasCurves()
            {
#warning will lock application if flash dump uncompressing fails
                return _deviceData.PointsSource != null
                    && _deviceData.PointsSource.PointsRows.Count > 0
                    && _deviceData.PointsSource.PointsRows[0].Points.Length > 0;
            }

            void exportToLas()
            {
                // ToDo: Run from dedicated thread
                var w = new LasExportWindow(_deviceData.PointsSource.CurveInfos, _deviceData.PointsSource.PointsRows);
                w.ShowDialog();
            }
        }
    }
}
