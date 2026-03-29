# NibbleHelper

Static helper methods for splitting and combining the two nibbles (4-bit halves) of a `byte`.

## Members

### CombineNibbles

```csharp
public static byte CombineNibbles(byte high, byte low);
```

Combines `high` and `low` into a single byte.

Throws `ArgumentOutOfRangeException` if either `high` or `low` exceeds `0x0F`.

### GetHighNibble

```csharp
public static byte GetHighNibble(byte value);
```

Extracts the value stored in the high nibble of `value`.

### GetLowNibble

```csharp
public static byte GetLowNibble(byte value);
```

Extracts the value stored in the low nibble of `value`.

### UnsetHighNibble

```csharp
public static byte UnsetHighNibble(byte value);
```

Returns a copy of `value` with the high nibble zeroed out.

### UnsetLowNibble

```csharp
public static byte UnsetLowNibble(byte value);
```

Returns a copy of `value` with the low nibble zeroed out.

## Examples

Splitting a byte into its two nibbles.

```csharp
byte val = 0xAB;

NibbleHelper.GetHighNibble(val); // 0x0A
NibbleHelper.GetLowNibble(val); // 0x0B
```

Combining two nibbles.

```csharp
byte result = NibbleHelper.CombineNibbles(0x0A, 0x0B);
// 0xAB
```

Clearing one nibble while preserving the other.

```csharp
byte val = 0xAB;

NibbleHelper.UnsetHighNibble(val); // 0x0B
NibbleHelper.UnsetLowNibble(val); // 0x00
```
