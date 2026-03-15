using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kwerty.DviZe.Bits;

public static class BitExtensions
{
    extension<T>(T number) where T : IBinaryInteger<T>
    {
        public bool IsBitSet(int idx)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(idx, nameof(idx));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx, Unsafe.SizeOf<T>() * 8, nameof(idx));

            return (number & (T.One << idx)) != T.Zero;
        }

        public T ReplaceBit(int idx, bool value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(idx, nameof(idx));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx, Unsafe.SizeOf<T>() * 8, nameof(idx));

            if (value)
            {
                return number | (T.One << idx);
            }
            else
            {
                return number & ~(T.One << idx);
            }
        }

        public T SetBit(int idx) => ReplaceBit(number, idx, value: true);

        public T UnsetBit(int idx) => ReplaceBit(number, idx, value: false);

        public static T FromBits(params bool[] bits)
        {
            var result = T.Zero;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result |= T.One << i;
                }
            }
            return result;
        }
    }
}