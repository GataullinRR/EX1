using System;
using System.Collections.Generic;

namespace DeviceBase.IOModels
{
    public class EntityLength : IEquatable<EntityLength>
    {
        public static implicit operator EntityLength(int length)
        {
            return new EntityLength(length);
        }

        public static readonly EntityLength TILL_THE_END_OF_A_PACKET = new EntityLength(null);

        readonly int? _Length;

        public int Length => IsTillTheEndOfAPacket
            ? throw new NotSupportedException("The length isn't fixed!")
            : _Length.Value;
        public bool IsTillTheEndOfAPacket => !_Length.HasValue;

        public EntityLength(int length)
            : this((int?)length)
        {

        }
        EntityLength(int? length)
        {
            _Length = length;
        }

        public override string ToString()
        {
            return IsTillTheEndOfAPacket
                ? nameof(TILL_THE_END_OF_A_PACKET)
                : Length.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityLength);
        }
        public bool Equals(EntityLength other)
        {
            return other != null &&
                   EqualityComparer<int?>.Default.Equals(_Length, other._Length) &&
                   IsTillTheEndOfAPacket == other.IsTillTheEndOfAPacket;
        }
        public override int GetHashCode()
        {
            return 800501978 + EqualityComparer<int?>.Default.GetHashCode(_Length);
        }
    }
}
