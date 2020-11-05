using DeviceBase.IOModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class StatusFeatureProviderProxy : RUSDeviceProxyBase
    {
        const string STATUS_MNEMONIC = "STAT";

        readonly StatusFeature _feature = new StatusFeature();

        public StatusFeatureProviderProxy(IRUSDevice @base) : base(@base)
        {

        }

        public override async Task<ReadResult> ReadAsync(Command request, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            var result = await base.ReadAsync(request, scope, cancellation);
            if (result.Status == ReadStatus.OK)
            {
                var statusField = result.Entities
                    ?.Where(e => e.Descriptor.Name == STATUS_MNEMONIC)
                    ?.FirstOrDefault()
                    ?.Value;
                if (statusField != null && statusField.GetType().IsOneOf(typeof(ushort), typeof(uint)))
                {
                    var size = Marshal.SizeOf(statusField) * 8;
                    var bits = Convert.ChangeType(statusField, TypeCode.UInt32).To<uint>();
                    var statusBitsInfo = new DeviceStatusInfo.StatusInfo(size, bits);
                    var statusInfo = new DeviceStatusInfo(Id, null, statusBitsInfo);
                    _feature.FireStatusAcquired(statusInfo);
                }
            }

            return result;
        }

        public override async Task<StatusReadResult> TryReadStatusAsync(DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            var result = await base.TryReadStatusAsync(scope, cancellation);
            if (result.StatusInfo != null)
            {
                _feature.FireStatusAcquired(result.StatusInfo);
            }

            return result;
        }

        public override T TryGetFeature<T>()
        {
            if (typeof(T) == typeof(StatusFeature))
            {
                return _feature as T;
            }
            else
            {
                // Allow base classes to resolve feature request
                return base.TryGetFeature<T>();
            }
        }
    }
}
