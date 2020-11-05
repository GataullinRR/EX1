using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels.Protocols
{
    static class SalachovProtocol
    {
        public static readonly Key HEADER_SECTION_KEY = nameof(HEADER_SECTION_KEY);
        public static readonly Key BODY_SECTION_KEY = nameof(BODY_SECTION_KEY);
    }
}
