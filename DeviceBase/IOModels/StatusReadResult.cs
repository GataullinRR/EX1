using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBase.IOModels
{
    public class StatusReadResult : ReadResult
    {
        internal static readonly new StatusReadResult UNKNOWN_ERROR = new StatusReadResult(ReadResult.UNKNOWN_ERROR, null);

        /// <summary>
        /// <see cref="null"/> if some error occured
        /// </summary>
        public DeviceStatusInfo StatusInfo { get; }

        internal StatusReadResult(ReadResult statusReadResult, DeviceStatusInfo statusInfo)
            : base(statusReadResult.Status, statusReadResult.Entities, statusReadResult.Response, statusReadResult.AnswerSections)
        {
            StatusInfo = statusInfo;
        }
    }
}
