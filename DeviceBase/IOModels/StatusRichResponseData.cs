using Common;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
    enum LastWriteRequestStatus : ushort
    {
        EXECUTED = 0x0000,
        ERROR = 0xFFFF,
        PROCESSING = 0xAAAA,
        BUSY = 0xBBBB,
    }

    class StatusRichResponseData : RichResponseDataBase
    {
        static readonly EntityDescriptor StatusDescriptor = new EntityDescriptor(
            "Operation status", 
            0, 
            EntityLength.TILL_THE_END_OF_A_PACKET, 
            DataEntityFormat.UINT16,
            v => v.To<ushort>().IsOneOf(EnumUtils.GetValues<LastWriteRequestStatus>().Select(v2 => v2.To<ushort>()).ToArray()));

        public LastWriteRequestStatus Status { get; }

        public StatusRichResponseData(IEnumerable<byte> body) : base(body)
        {
            var raw = (ushort)StatusDescriptor.Deserialize(body);
            Status = (LastWriteRequestStatus)raw;
            if (StatusDescriptor.ValidateValueRange(raw))
            {
                Logger.LogInfo(null, $"Получен статус операции: {Status}");
            }
            else
            {
                Logger.LogErrorEverywhere($"Некорректный статус операции: {raw:X4}");
            }
        }

        public IEnumerable<byte> Serialize()
        {
            return StatusDescriptor.Serialize(Status);
        }
    }
}
