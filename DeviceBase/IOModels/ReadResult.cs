using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    public enum ReadStatus
    {
        [RequestStatusMapping(RequestStatus.OK)]
        OK,
        [RequestStatusMapping(RequestStatus.WRONG_CHECKSUM, 
            RequestStatus.WRONG_HEADER, 
            RequestStatus.WRONG_LENGTH, 
            RequestStatus.NOT_EXPECTED_RESPONSE_BYTES, 
            RequestStatus.DESERIALIZATION_ERROR, 
            RequestStatus.READ_TIMEOUT, 
            RequestStatus.UNKNOWN_ERROR,
            RequestStatus.CONNECTION_INTERFACE_ERROR)]
        REQUEST_ERROR,
        [RequestStatusMapping(RequestStatus.NOT_PERFORMED)]
        NOT_PERFORMED,
        [RequestStatusMapping(RequestStatus.CANCELLED)]
        CANCELLED,

        DESERIALIZATION_FAILURE,
        DEVICE_REPORTED_ERROR,
        NOT_SUPPORTED_BY_INTERFACE,
        COULD_NOT_INITIALIZE,
        UNKNOWN_ERROR
    }

    public class ReadResult
    {
        internal static readonly ReadResult UNKNOWN_ERROR = new ReadResult(ReadStatus.UNKNOWN_ERROR, Enumerable.Empty<IDataEntity>(), ResponseData.NONE);

        public ReadStatus Status { get; }
        /// <summary>
        /// Can be <see cref="null"/> if request format has no notion for <see cref="IDataEntity"/>
        /// </summary>
        public IEnumerable<IDataEntity> Entities { get; }
        /// <summary>
        /// Can be <see cref="null"/> if request was not performed
        /// </summary>
        public IResponseData Response { get; }
        /// <summary>
        /// Never null
        /// </summary>
        public IDictionary<Key, IResponseData> AnswerSections { get; }

        public ReadResult(ReadStatus status, IEnumerable<IDataEntity> entities, IResponseData answer)
            : this(status, entities, answer, new Dictionary<Key, IResponseData>())
        {

        }
        public ReadResult(ReadStatus status, IEnumerable<IDataEntity> entities, IResponseData answer, IDictionary<Key, IResponseData> answerSections)
        {
            Status = status;
            Entities = entities;
            Response = answer;
            AnswerSections = answerSections ?? throw new ArgumentNullException(nameof(answerSections));
        }
    }
}
