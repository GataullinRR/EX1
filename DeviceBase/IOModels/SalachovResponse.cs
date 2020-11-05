using System.Collections.Generic;
using DeviceBase.IOModels.Protocols;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class SalachovResponse : ResponseBase<SalachovRequest>
    {
        public IResponseData Header => DataSections[SalachovProtocol.HEADER_SECTION_KEY];
        public IResponseData Body => DataSections[SalachovProtocol.BODY_SECTION_KEY];

        public SalachovResponse(SalachovRequest request, RequestStatus status, IResponseData data, IResponseData headerSection, IResponseData bodySection) 
            : base(request, status, data, 
                  new Dictionary<Key, IResponseData>() 
                  { 
                      { SalachovProtocol.HEADER_SECTION_KEY, headerSection },
                      { SalachovProtocol.BODY_SECTION_KEY, bodySection } 
                  })
        {
            
        }
    }
}
