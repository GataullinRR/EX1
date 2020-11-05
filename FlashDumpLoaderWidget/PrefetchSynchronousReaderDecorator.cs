using Common;
using DataViewExports;
using DeviceBase.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Utilities.Types;
using Vectors;

namespace FlashDumpLoaderWidget
{
    class PrefetchSynchronousReaderDecorator : ReaderProxyBase
    {
        const int BUFFER_LENGTH = 10000;

        object _locker = new object();
        long _bufferStart = -1;
        IPointsRow[] _buffer;

        public PrefetchSynchronousReaderDecorator(IRowsReader @base) : base(@base)
        {

        }

        /// <summary>
        /// Can cause deadlock. Do not use from UI thread!
        /// </summary>
        /// <param name="firstRowIndex"></param>
        /// <returns></returns>
        public IPointsRow ReadRow(int firstRowIndex)
        {
            lock (_locker)
            {
                if (_bufferStart == -1 || !new IntInterval(_bufferStart, _bufferStart + _buffer.Length - 1).Contains(firstRowIndex))
                {
                    try
                    {
                        var bufferFuture = ReadRowsAsync(firstRowIndex, BUFFER_LENGTH, new AsyncOperationInfo());
                        _buffer = bufferFuture.Result;
                        _bufferStart = firstRowIndex;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(null, $"Ошибка чтения строк с позиции: {firstRowIndex}", ex);

                        _bufferStart = -1;
                        return null;
                    }
                }

                return _buffer[firstRowIndex - _bufferStart];
            }
        }

        //void doEvents()
        //{
        //    Application.Current.Dispatcher.Invoke(
        //        DispatcherPriority.Background,
        //        new ThreadStart(delegate { }));
        //}

        public override Task<IPointsRow[]> ReadRowsAsync(int firstRowIndex, int rowsCount, AsyncOperationInfo operationInfo)
        {
            return base.ReadRowsAsync(firstRowIndex, rowsCount, operationInfo);
        }
    }
}
