using Common;
using DataViewExports;
using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using DeviceBase.Models;
using MVVMUtilities.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace FlashDumpLoaderWidget
{
    /// <summary>
    /// Is used asynchronously. Sync methods are implemented only for comportability reasons
    /// </summary>
    class FileMappedPointsRowsReadOnlyCollection : IEnhancedObservableCollection<IPointsRow>, IRowsReaderProvider
    {
        readonly PrefetchSynchronousReaderDecorator _reader;
        public IRowsReader RowsReader => _reader;

        public IPointsRow this[int index] 
        { 
            get => _reader.ReadRow(index); 
            set => throw new NotSupportedException(); 
        }
        IPointsRow IReadOnlyList<IPointsRow>.this[int index] => this[index];
        object IList.this[int index] 
        { 
            get => this[index]; 
            set => throw new NotSupportedException();
        }

        public bool IsReadOnly => true;

        public int Count => _reader.RowsCount;
        public bool IsFixedSize => true;
        public object SyncRoot { get; } = new object();
        public bool IsSynchronized => false;

        public IDisposable EventSuppressingMode => new DisposingAction(() => { });
        public IDisposable ItemChangesEventIgnoringModeHolder => new DisposingAction(() => { });
        public IDisposable ItemChangesEventSuppressingModeHolder => new DisposingAction(() => { });

        public event EventHandler<ItemPropertyChangedEventArgs<IPointsRow>> ItemPropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public FileMappedPointsRowsReadOnlyCollection(IRowsReader reader)
        {
            _reader = new PrefetchSynchronousReaderDecorator(reader);
        }

        public void Add(IPointsRow item)
        {
            throw new NotSupportedException();
        }

        public int Add(object value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(IPointsRow item)
        {
            throw new NotSupportedException();
        }

        public bool Contains(object value)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(IPointsRow[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<IPointsRow> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        public int IndexOf(IPointsRow item)
        {
            throw new NotSupportedException();
        }

        public int IndexOf(object value)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, IPointsRow item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        public bool Remove(IPointsRow item)
        {
            throw new NotSupportedException();
        }

        public void Remove(object value)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Task<IPointsRow[]> GetRangeAsync(int from, int count, AsyncOperationInfo operationInfo)
        {
            return _reader.ReadRowsAsync(from, count, operationInfo);
        }
    }
}
