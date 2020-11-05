using System;
using DeviceBase.Devices;
using Utilities.Extensions;

namespace VirtualDevice
{
    abstract class VirtualRUSModuleBase : VirtualRUSDeviceBase, IRUSModule
    {
        public IOStream ChildrenInterfaceLine { get; set; }

        public VirtualRUSModuleBase(WordSerializator serializator) : base(serializator)
        {

        }

        [SalachovCommandHandlerAttribute(Command.RETRANSLATE_PACKET)]
        public byte[] HandleRetranslatePacket(byte[] requestBody, bool isReadRequest)
        {
            if (isReadRequest)
            {
                throw new NotSupportedException();
            }
            else
            {
                ChildrenInterfaceLine.Write(requestBody);
                var answer = ChildrenInterfaceLine.PopInputBuffer();

                return answer;
            }
        }
    }
}
