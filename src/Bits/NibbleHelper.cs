using System;

namespace Kwerty.DviZe.Bits;

public static class NibbleHelper
{
    public static byte CombineNibbles(byte high, byte low)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(high, 0x0F, nameof(high));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(low, 0x0F, nameof(low));
        return (byte)((high << 4) | low);
    }

    public static byte GetHighNibble(byte value)
    {
        return (byte)((value >> 4) & 0x0F);
    }

    public static byte GetLowNibble(byte value)
    {
        return (byte)(value & 0x0F);
    }

    public static byte UnsetHighNibble(byte value) => GetLowNibble(value);

    public static byte UnsetLowNibble(byte value)
    {
        return (byte)(value & 0xF0);
    }
}