using Common;
using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;
using WPFUtilities.Types;

namespace Exporters.Las
{
    public class LasExportMainVM : INotifyPropertyChanged
    {
        readonly IList<IPointsRow> _rows;
        readonly TaskCancellationManager _cancellationManager = new TaskCancellationManager();

        public event PropertyChangedEventHandler PropertyChanged;

        public ActionCommand SelectAll { get; }
        public ActionCommand UnselectAll { get; }
        public ActionCommand Export { get; }
        public ActionCommand Cancel { get; }
        public IList<ICurveInfo> CurveInfos { get; }
        public RichProgress ExportProgress { get; } = new RichProgress();

        public LasExportMainVM(IList<IPointsRow> rows, IEnumerable<ICurveInfo> curveInfos)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            CurveInfos = curveInfos?.Select(ci => new CurveInfo(ci.Title, ci.IsShown)).ToArray() ?? throw new ArgumentNullException(nameof(curveInfos));

            SelectAll = new ActionCommand(selectAll, () => !_cancellationManager.HasTasks, _cancellationManager);
            UnselectAll = new ActionCommand(unselectAll, () => !_cancellationManager.HasTasks, _cancellationManager);
            Export = new ActionCommand(exportAsync, () => !_cancellationManager.HasTasks, _cancellationManager);
            Cancel = new ActionCommand(TryCancelExportAsync, () => _cancellationManager.HasTasks, _cancellationManager);

            void selectAll()
            {
                foreach (var ci in CurveInfos)
                {
                    ci.IsShown = true;
                }
            }
            void unselectAll()
            {
                foreach (var ci in CurveInfos)
                {
                    ci.IsShown = false;
                }
            }

            async Task exportAsync()
            {
                var file = tryRequestFile();
                if (file.Stream != null)
                {
                    using (file.Stream)
                    {
                        try
                        {
                            ExportProgress.Reset();

                            var exportingCurvesIndexes = CurveInfos.FindAllIndexes(ci => ci.IsShown).ToArray();
                            var curvesNames = CurveInfos.Get(exportingCurvesIndexes).Select(ci => ci.Title).ToArray();
                            var filteredRows = _rows.GetRange(0, _rows.Count)
                                .Select(r => r.Points.Get(exportingCurvesIndexes).ToArray());
                            var task = new LasExporter()
                                .SaveToAsync(file.Stream, curvesNames, filteredRows, _rows.Count, new AsyncOperationInfo(ExportProgress, _cancellationManager.Token));
                            await _cancellationManager.RegisterAndReturn(task);

                            UserInteracting.ReportSuccess("Экспорт в Las", "Экспорт успешно завершен");
                        }
                        catch (OperationCanceledException)
                        {
                            file.Stream.Dispose();
                            CommonUtils.Try(() => File.Delete(file.Path));

                            ExportProgress.Stage = "Операция прервана";
                            ExportProgress.OperationStatus = OperationStatus.ABORTED;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorEverywhere("Ошибка экспорта в Las", ex);
                            Reporter.ReportError("Не удалось произвести экспорт в Las", ex);
                            ExportProgress.Stage = "Ошибка экспорта";
                            ExportProgress.OperationStatus = OperationStatus.FAILED;

                            UserInteracting.ReportError("Экспорт в Las", "Неизвестная ошибка экспорта");

                            await TryCancelExportAsync();
                        }
                        finally
                        {
                            _cancellationManager.Reset();
                        }
                    }
                }

                (Stream Stream, string Path) tryRequestFile()
                {
                    var filePath = IOUtils.RequestFileSavingPath("LAS (*las)|*las");
                    return (CommonUtils.TryOrDefault(() =>
                    {
                        if (!Path.HasExtension(filePath))
                        {
                            filePath += ".las";
                        }
                        File.Delete(filePath);
                        return File.OpenWrite(filePath);
                    }), filePath);
                }
            }
        }

        public async Task TryCancelExportAsync()
        {
            try
            {
                _cancellationManager.Cancel();
                await _cancellationManager.WaitForAllToCancelAsync();
            }
            catch (Exception ex)
            {
                Logger.LogErrorEverywhere("Ошибка отмены экспорта", ex);
            }
        }
    }
}
