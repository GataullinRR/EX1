using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public enum BurnStatus
    {
        [RequestStatusMapping(RequestStatus.OK)]
        OK,
        [RequestStatusMapping(RequestStatus.NOT_PERFORMED)]
        NOT_PERFORMED,
        [RequestStatusMapping(RequestStatus.CONNECTION_INTERFACE_ERROR, 
            RequestStatus.NOT_EXPECTED_RESPONSE_BYTES, 
            RequestStatus.READ_TIMEOUT, 
            RequestStatus.DESERIALIZATION_ERROR, 
            RequestStatus.UNKNOWN_ERROR, 
            RequestStatus.WRONG_CHECKSUM, 
            RequestStatus.WRONG_HEADER, 
            RequestStatus.WRONG_LENGTH)]
        REQUEST_ERROR,
        [RequestStatusMapping(RequestStatus.CANCELLED)]
        CANCELED,

        VERIFICATION_ERROR,
        FORBIDDEN_SERIAL,
        NOT_SUPPORTED_BY_INTERFACE,
        COULD_NOT_INITIALIZE,
        COULD_NOT_COMPLETE_WAIT_OPERATION,
        /// <summary>
        /// Device reported error state (see <see cref="StatusRichResponseData"/>)
        /// </summary>
        ERROR_ON_DEVICE,
        /// <summary>
        /// Device reported busy state (see <see cref="StatusRichResponseData"/>)
        /// </summary>
        DEVICE_IS_BUSY,
        /// <summary>
        /// Device reported executing state (see <see cref="StatusRichResponseData"/>)
        /// </summary>
        EXECUTING,
        /// <summary>
        /// An exception occured
        /// </summary>
        UNKNOWN_ERROR
    }

    public class BurnResult
    {
        public BurnStatus Status { get; }
        public IResponseData Response { get; }
        public IDictionary<Key, IResponseData> ResponseSections { get; }

        public BurnResult(BurnStatus status, IResponseData response)
            : this(status, response, new Dictionary<Key, IResponseData>())
        {

        }
        public BurnResult(BurnStatus status, IResponseData response, IDictionary<Key, IResponseData> responseSections)
        {
            Status = status;
            Response = response;
            ResponseSections = responseSections ?? throw new ArgumentNullException(nameof(responseSections));
        }
    }
}
