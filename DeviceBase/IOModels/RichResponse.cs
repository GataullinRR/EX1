using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;

namespace DeviceBase.IOModels
{
    enum RichResponseType : ushort
    {
        OPERATION_STATUS = 0
    }

    class RichResponse
    {
        static readonly EntityDescriptor TypeDescriptor = new EntityDescriptor(
            "Rich response type",
            0,
            2,
            DataEntityFormat.UINT16,
            v => v.To<ushort>().IsOneOf(EnumUtils.GetValues<RichResponseType>().Select(v2 => v2.To<ushort>()).ToArray()));

        public RichResponseType Type { get; }
        public IEnumerable<byte> Body { get; }
        /// <summary>
        /// Not null for <see cref="RichResponseType.OPERATION_STATUS"/>
        /// </summary>
        public StatusRichResponseData StatusResponseData { get; } 

        public RichResponse(IEnumerable<byte> packetBody)
        {
            var raw = (ushort)TypeDescriptor.Deserialize(packetBody);
            Type = (RichResponseType)raw;
            if (TypeDescriptor.ValidateValueRange(raw))
            {
                Logger.LogInfo(null, $"Получен ответ типа: {Type}");

                switch (Type)
                {
                    case RichResponseType.OPERATION_STATUS:
                        StatusResponseData = new StatusRichResponseData(packetBody.Skip(TypeDescriptor.Length.Length));
                        break;
                 
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                Logger.LogErrorEverywhere($"Некорректный тип ответа: {raw:X4}");
            }
        }
    }
}
