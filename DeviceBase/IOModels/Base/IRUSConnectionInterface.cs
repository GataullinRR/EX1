using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public enum InterfaceDevice
    {
        /// <summary>
        /// Not yet opened
        /// </summary>
        NOT_SET,
        COM,
        RUS_TECHNOLOGICAL_MODULE_FTDI_BOX
    }

    public enum WaitMode
    {
        EXACT,
        NO_MORE_THAN
    }

    /// <summary>
    /// Thread safe.
    /// </summary>
    public interface IResponseFuture
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="Exception"></exception>
        Task<byte[]> WaitAsync(int count, WaitMode waitMode, AsyncOperationInfo operationInfo);
    }

    /// <summary>
    /// Methods should be thread safe
    /// </summary>
    public interface IRUSConnectionInterface
    {
        event EventHandler ConnectionEstablished;
        event EventHandler ConnectionClosed;

        /// <summary>
        /// Can change
        /// </summary>
        InterfaceDevice InterfaceDevice { get; }
        IEnumerable<Protocol> SupportedProtocols { get; }

        Task<IResponse> RequestAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo operationInfo);

        /// <summary>
        /// Aquires lock (synchronization) on the device's methods
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task<IDisposable> LockAsync(CancellationToken cancellation);
    }
}
