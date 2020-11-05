using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceBase.Devices;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
    /// <summary>
    /// The idea is to provide capability to send/get settings/data to/from any place inside class hierarchy.
    /// The instance is scoped to single operation with <see cref="IRUSDevice"/> and must not have any side-effects on following operations
    /// </summary>
    public class DeviceOperationScope
    {
        public static readonly DeviceOperationScope DEFAULT = new DeviceOperationScope(new IRequestParameter[0]);

        public IRequestParameter[] Parameters { get; }

        public DeviceOperationScope(params IRequestParameter[] parameters)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public T TryGetFirstParameter<T>()
            where T : class, IRequestParameter
        {
            return Parameters
                .Select(p => p as T)
                .SkipNulls()
                .FirstOrDefault();
        }
    }
}
