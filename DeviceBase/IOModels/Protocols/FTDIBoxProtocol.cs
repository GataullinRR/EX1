using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels.Protocols
{
    static class FTDIBoxProtocol
    {
        public static readonly Key SERVICE_DATA_SECTION_KEY = nameof(SERVICE_DATA_SECTION_KEY);
        public static readonly Key BODY_SECTION_KEY = nameof(BODY_SECTION_KEY);
    }
}
