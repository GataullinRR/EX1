using DeviceBase.Helpers;
using System;
using System.Collections.Generic;
using Utilities.Extensions;
using Vectors;

namespace DeviceBase.IOModels
{
    /// <summary>
    /// It is used as <see cref="IDataEntity.Value"/>
    /// </summary>
    public class DataPacketEntityDescriptor
    {
        /// <summary>
        /// Mnemonic
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The full length in bytes
        /// </summary>
        public int Length { get; }
        /// <summary>
        /// From the beginning of data packet's body. Though it starts from 1, not from 0.
        /// </summary>
        public int Position { get; }
        public int NumOfSignificantBits { get; }
        public bool IsSigned { get; }

        internal DataPacketEntityDescriptor
            (string name, int length, int position, byte isSignedAndNumOfSignificantBits)
            : this(name, length, position,
                  (~0x80) & isSignedAndNumOfSignificantBits,
                  (0x80 & isSignedAndNumOfSignificantBits) > 0)
        {

        }
        public DataPacketEntityDescriptor(string name, int length, int position, bool isSigned)
            : this(name, length, position, (length * 8 - (isSigned ? 1 : 0)).ToByte(), isSigned)
        {

        }
        public DataPacketEntityDescriptor
            (string name, int length, int position, int numOfSignificantBits, bool isSigned)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Length = length;
            Position = position;
            NumOfSignificantBits = numOfSignificantBits;
            IsSigned = isSigned;
        }

        public EntityDescriptor GetDescriptor()
        {
            var format = getFormat();
            return new EntityDescriptor(Name, Position - 1, Length, format, format.GetDefaultValidator());

            ////////////////////////////////////////

            DataEntityFormat getFormat()
            {
                switch (Length)
                {
                    case 1:
                        return IsSigned ? DataEntityFormat.INT8 : DataEntityFormat.UINT8;
                    case 2:
                        return IsSigned ? DataEntityFormat.INT16 : DataEntityFormat.UINT16;
                    case 4:
                        return IsSigned ? DataEntityFormat.INT32 : DataEntityFormat.UINT32;
                    case 8:
                        return IsSigned ? DataEntityFormat.INT64 : DataEntityFormat.UINT64;

                    default:
                        return DataEntityFormat.BYTE_ARRAY;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var descriptor = obj as DataPacketEntityDescriptor;
            return descriptor != null &&
                   Name == descriptor.Name &&
                   Length == descriptor.Length &&
                   Position == descriptor.Position &&
                   NumOfSignificantBits == descriptor.NumOfSignificantBits &&
                   IsSigned == descriptor.IsSigned;
        }

        public override int GetHashCode()
        {
            var hashCode = -1321573025;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + Position.GetHashCode();
            hashCode = hashCode * -1521134295 + NumOfSignificantBits.GetHashCode();
            hashCode = hashCode * -1521134295 + IsSigned.GetHashCode();
            return hashCode;
        }
    }
}
