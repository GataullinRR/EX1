using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    abstract class RUSConnectionInterfaceProxyBase : IRUSConnectionInterface
    {
        protected readonly IRUSConnectionInterface _base;

        public InterfaceDevice InterfaceDevice => _base.InterfaceDevice;

        public virtual IEnumerable<Protocol> SupportedProtocols => _base.SupportedProtocols;

        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;

        protected RUSConnectionInterfaceProxyBase(IRUSConnectionInterface @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));

            _base.ConnectionClosed += _base_ConnectionClosed;
            _base.ConnectionEstablished += _base_ConnectionEstablished;
        }

        void _base_ConnectionEstablished(object sender, EventArgs e)
        {
            ConnectionEstablished?.Invoke(this, e);
        }

        void _base_ConnectionClosed(object sender, EventArgs e)
        {
            ConnectionClosed?.Invoke(this, e);
        }

        public virtual Task<IResponse> RequestAsync(IRequest request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            return _base.RequestAsync(request, scope, cancellation);
        }

        public Task<IDisposable> LockAsync(CancellationToken cancellation)
        {
            return _base.LockAsync(cancellation);
        }
    }
}
