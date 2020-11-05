using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.Devices;
using DeviceBase.Attributes;
using DeviceBase.IOModels;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace DeviceBase.Helpers
{
    public static class Extensions
    {
        class ResponseToStreamAdapter : Stream
        {
            readonly IResponseData _response;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;

            public override long Length => _response.Count;
            public override long Position { get; set; }

            public ResponseToStreamAdapter(IResponseData response)
            {
                _response = response ?? throw new ArgumentNullException(nameof(response));
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("Use ReadAsync instead");
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var maxCount = Length - Position;
                count = (int)Math.Min(maxCount, count);
                var data = await _response.GetRangeAsync(Position, count, cancellationToken);
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                Position += data.Length;

                return data.Length;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        public static bool IsFileRequest(this Command request)
        {
            return request.GetInfo().IsFileRequest;
        }

        public static FileType GetFileType(this Command request)
        {
            return request.GetInfo().FileType.Value;
        }

        public static int GetSize(this DataTypes dataType)
        {
            return dataType.GetAttribute<SizeAttribute>().Size;
        }

        public static RequestAddressInfoAttribute GetInfo(this Command requestAddress)
        {
            return requestAddress.GetAttribute<RequestAddressInfoAttribute>();
        }

        public static ResponseInfoAttribute GetInfo(this ReadResponse entity)
        {
            return entity.GetAttribute<ResponseInfoAttribute>();
        }
        public static ResponseInfoAttribute GetInfo(this WriteResponse entity)
        {
            return entity.GetAttribute<ResponseInfoAttribute>();
        }

        public static FileTypeInfoAttribute GetInfo(this FileType fileType)
        {
            return fileType.GetAttribute<FileTypeInfoAttribute>();
        }

        internal static InclinometrModeInfoAttribute GetInfo(this InclinometrMode mode)
        {
            return mode.GetAttribute<InclinometrModeInfoAttribute>();
        }

        public static Command GetRequestAddress(this FileType fileType)
        {
            return fileType.GetInfo().RequestAddress;
        }

        internal static Func<dynamic, bool> GetDefaultValidator(this DataEntityFormat entityFormat)
        {
            switch (entityFormat)
            {
                case DataEntityFormat.BOOLEAN:
                    return x => x == true || x == false;
                case DataEntityFormat.INT8:
                    return x => x >= sbyte.MinValue && x <= sbyte.MaxValue;
                case DataEntityFormat.INT16:
                    return x => x >= short.MinValue && x <= short.MaxValue;
                case DataEntityFormat.INT32:
                    return x => x >= int.MinValue && x <= int.MaxValue;
                case DataEntityFormat.INT64:
                    return x => x >= long.MinValue && x <= long.MaxValue;
                case DataEntityFormat.UINT8:
                    return x => x >= byte.MinValue && x <= byte.MaxValue;
                case DataEntityFormat.UINT16:
                    return x => x >= ushort.MinValue && x <= ushort.MaxValue;
                case DataEntityFormat.UINT32:
                    return x => x >= uint.MinValue && x <= uint.MaxValue;
                case DataEntityFormat.UINT64:
                    return x => x >= ulong.MinValue && x <= ulong.MaxValue;

                case DataEntityFormat.BYTE_ARRAY:
                    return x => true;
                case DataEntityFormat.UINT16_ARRAY:
                    return x => true;
                case DataEntityFormat.ASCII_STRING:
                    return x => true;
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    return x => true;
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                    return x => true;

                default:
                    throw new NotSupportedException();
            }
        }

        public static bool IsInteger(this DataEntityFormat entityFormat)
        {
            switch (entityFormat)
            {
                case DataEntityFormat.INT8:
                case DataEntityFormat.INT16:
                case DataEntityFormat.INT32:
                case DataEntityFormat.INT64:
                case DataEntityFormat.UINT8:
                case DataEntityFormat.UINT16:
                case DataEntityFormat.UINT32:
                case DataEntityFormat.UINT64:
                    return true;

                case DataEntityFormat.BOOLEAN:
                case DataEntityFormat.ASCII_STRING:
                case DataEntityFormat.BYTE_ARRAY:
                case DataEntityFormat.UINT16_ARRAY:
                case DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY:
                case DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY:
                    return false;

                default:
                    throw new NotSupportedException();
            }
        }

        public static async Task<byte[]> GetRangeSafeAsync(this IResponseData responseData, long from, int count, AsyncOperationInfo operationInfo)
        {
            var hasFrom = from < responseData.Count;
            count = (int)Math.Min(count, (responseData.Count - from));

            return hasFrom
                ? await responseData.GetRangeAsync(from, count, operationInfo)
                : new byte[0];
        }

        public static Stream ToStream(this IResponseData responseData)
        {
            return new ResponseToStreamAdapter(responseData);
        }
    }
}
