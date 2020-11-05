using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utilities.Extensions;
using Utilities.Types;

namespace WidgetsCompositionRoot
{
    class DIContainer : Disposable, IDIContainer
    {
        /// <summary>
        /// Because dictonary's cant be null
        /// </summary>
        static readonly object NULL_OBJECT = new object();
        /// <summary>
        /// (service type, (scope, services))
        /// </summary>
        readonly Dictionary<Type, Dictionary<object, List<object>>> _services = new Dictionary<Type, Dictionary<object, List<object>>>();

        public void Register<T>(T service, object scope = null) where T : class
        {
            throwIfDisposed();
            
            if (service == null)
            {
                throw new ArgumentNullException();
            }

            scope = scope ?? NULL_OBJECT;
            var t = typeof(T);
            _services.EnsureKeyExists(t, new Dictionary<object, List<object>>());
            _services[t].EnsureKeyExists(scope, new List<object>());
            _services[t][scope].Add(service);
        }

        public T ResolveSingle<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            var service = TryResolveSingle<T>(scope);
            if (service == null)
            {
                throw new ServiceIsNotYetAwailableException(typeof(T), scope);
            }
            else
            {
                return service;
            }
        }
        public IEnumerable<T> ResolveAll<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            var service = TryResolveAll<T>(scope);
            if (service == null)
            {
                throw new ServiceIsNotYetAwailableException(typeof(T), scope);
            }
            else
            {
                return service;
            }
        }

        public T TryResolveSingle<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            var all = TryResolveAll<T>(scope);
            return all?.Count() == 1
                ? all.Single()
                : null;
        }
        public IEnumerable<T> TryResolveAll<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            scope = scope ?? NULL_OBJECT;
            var t = typeof(T);
            var hasAnyServices = _services.ContainsKey(t) && _services[t].Any(ss => ss.Value.Count > 0);
            if (hasAnyServices)
            {
                return scope == NULL_OBJECT
                    ? _services[t].Select(ss => ss.Value).Flatten().Select(s => s.To<T>()).ToArray() // Select all
                    : _services[t].ContainsKey(scope)
                        ? _services[t][scope].Count > 0 
                            ? _services[t][scope].Select(s => s.To<T>()).ToArray() 
                            : null
                        : null;
            }
            else
            {
                return null;
            }
        }

        public void RemoveRegistration<T>(object scope = null) where T : class
        {
            throwIfDisposed();

            scope = scope ?? NULL_OBJECT;
            var t = typeof(T);
            if (scope == NULL_OBJECT)
            {
                if (_services.ContainsKey(t))
                {
                    _services.Remove(t);
                }
            }
            else
            {
                if (_services.ContainsKey(t) && _services[t].ContainsKey(scope))
                {
                    _services[t].Remove(scope);
                }
            }
        }

        protected override void disposeManagedState()
        {
            _services.Clear();
        }
    }
}
