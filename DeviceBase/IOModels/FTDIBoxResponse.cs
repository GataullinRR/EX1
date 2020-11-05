using System.Collections.Generic;
using DeviceBase.IOModels.Protocols;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    class FTDIBoxResponse : ResponseBase<FTDIBoxRequest>
    {
        public IResponseData ServiceData => DataSections[FTDIBoxProtocol.SERVICE_DATA_SECTION_KEY];
        public IResponseData Body => DataSections[FTDIBoxProtocol.BODY_SECTION_KEY];

        public FTDIBoxResponse(FTDIBoxRequest request, RequestStatus status, IResponseData data, IResponseData serviceSection, IResponseData bodySection)
            : base(request, status, data,
                  new Dictionary<Key, IResponseData>()
                  {
                      { FTDIBoxProtocol.SERVICE_DATA_SECTION_KEY, serviceSection },
                      { FTDIBoxProtocol.BODY_SECTION_KEY, bodySection }
                  })
        {

        }
    }
}
