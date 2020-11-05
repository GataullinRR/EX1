using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ResponseInfoAttribute : Attribute
    {
        public ResponseLength FullResponseLength { get; }
        public bool HasLengthField => FullResponseLength.IsUnknown;

        public ResponseInfoAttribute(int length)
        {
            FullResponseLength = length == -1
                ? ResponseLength.UNKNOWN
                : new ResponseLength(length);
        }
    }

    public enum ReadResponse
    {
        NOT_EXISTS,
        /// <summary>
        /// Just an array of bytes
        /// </summary>
        [ResponseInfo(-1)]
        BINARY_FILE,
        /// <summary>
        /// Response with length (for read and some custom write commands)
        /// </summary>
        [ResponseInfo(-1)]
        CUSTOM_WITH_LENGTH,
        /// <summary>
        /// Enhanced response for write commands. Created to add support for statuses and anything else.
        /// </summary>
        [ResponseInfo(-1)]
        RICH
    }

    public enum WriteResponse
    {
        NOT_EXISTS,
        /// <summary>
        /// Basic 6-byte response
        /// </summary>
        [ResponseInfo(6)]
        ACKNOWLEDGEMENT,
        /// <summary>
        /// Response with length (for read and some custom write commands)
        /// </summary>
        [ResponseInfo(-1)]
        CUSTOM_WITH_LENGTH,
        /// <summary>
        /// Enhanced response for write commands. Created to add support for statuses and anything else.
        /// </summary>
        [ResponseInfo(-1)]
        RICH
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequestAddressInfoAttribute : Attribute
    {
        readonly RUSDeviceId[] _supportedDevices;

        public bool IsCommand;
        public string CommandName = "NOT SET";
        public bool CanBeBroadcasted = false;
        public Protocol Protocol = Protocol.SALAHOV;

        public bool IsReadSupported => ReadResponse != ReadResponse.NOT_EXISTS;
        public bool IsWriteSupported => WriteResponse != WriteResponse.NOT_EXISTS;
        internal bool IsFileRequest => FileType.HasValue;
        internal ReadResponse ReadResponse { get; }
        internal WriteResponse WriteResponse { get; }
        internal FileType? FileType { get; }
        public byte Address { get; }

        public RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse)
            : this(address, readResponse, writeResponse, (FileType?)null)
        {

        }
        public RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse, params RUSDeviceId[] supportedDevices)
            : this(address, readResponse, writeResponse, null, supportedDevices)
        {

        }
        public RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse, FileType fileType)
            : this (address, readResponse, writeResponse, (FileType?)fileType)
        {

        }
        public RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse, FileType fileType, params RUSDeviceId[] supportedDevices)
            : this(address, readResponse, writeResponse, (FileType?)fileType, supportedDevices)
        {

        }
        RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse, FileType? fileType)
            : this(address, readResponse, writeResponse, fileType, null)
        {

        }
        RequestAddressInfoAttribute(byte address, ReadResponse readResponse, WriteResponse writeResponse, FileType? fileType, RUSDeviceId[] supportedDevices)
        {
            Address = address;
            FileType = fileType;
            ReadResponse = readResponse;
            WriteResponse = writeResponse;
            _supportedDevices = supportedDevices;
        }

        public bool IsSupportedForDevice(RUSDeviceId deviceId)
        {
            return _supportedDevices == null
                ? true
                : _supportedDevices.Contains(deviceId);
        }
    }
}
