using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utilities;
using Utilities.Types;

namespace Common
{
    /// <summary>
    /// Simple DI container. One instance for each device should be created
    /// </summary>
    public interface IDIContainer : IDisposable
    {
        /// <summary>
        /// Implementation should allow registration only during widgets construction
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        void Register<T>(T service, object scope = null) where T : class;
        T TryResolveSingle<T>(object scope = null) where T : class;
        /// <summary>
        /// Same as <see cref="TryResolveSingle{T}"/>, but throws exception when there is no such feature 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ServiceIsNotYetAwailableException">When there is no such service registered</exception>
        T ResolveSingle<T>(object scope = null) where T : class;

        /// <summary>
        /// Never returns an empty array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ServiceIsNotYetAwailableException">When there is no such service registered</exception>
        IEnumerable<T> ResolveAll<T>(object scope = null) where T : class;
        IEnumerable<T> TryResolveAll<T>(object scope = null) where T : class;

        #warning Should not be the part of the interface
        void RemoveRegistration<T>(object scope = null) where T : class;
    }
}
