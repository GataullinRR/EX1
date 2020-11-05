using System;
using System.Collections.Generic;
using Utilities.Types;

namespace Common
{
    public class DIContainerProxyBase : Disposable, IDIContainer
    {
        readonly IDIContainer _base;

        public DIContainerProxyBase(IDIContainer @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual void Register<T>(T service, object scope = null) where T : class
        {
            throwIfDisposed();

            _base.Register(service, scope);
        }

        public virtual T TryResolveSingle<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            return _base.TryResolveSingle<T>(scope);
        }

        public virtual T ResolveSingle<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            return _base.ResolveSingle<T>(scope);
        }

        public virtual IEnumerable<T> ResolveAll<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            return _base.ResolveAll<T>(scope);
        }

        public virtual IEnumerable<T> TryResolveAll<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            return _base.TryResolveAll<T>(scope);
        }

        protected override void disposeManagedState()
        {
            _base.Dispose();
        }

        public virtual void RemoveRegistration<T>(object scope = null) where T : class
        {
            _base.RemoveRegistration<T>(scope);
        }
    }
}
