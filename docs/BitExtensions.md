# BitExtensions

Extension methods for reading and manipulating the individual bits of a number type `T`.

```csharp
public bool IsBitSet(int idx);
```

Returns `true` if the bit at position `idx` is `1`.

ℹ️ `idx` is zero-based, where `0` is the least significant bit.

Throws `ArgumentOutOfRangeException` if `idx` is negative or exceeds the bit width of `T`.

```csharp
public T ReplaceBit(int idx, bool value);
```

Returns a copy of the number with the bit at position `idx` set according to `value`.

```csharp
public T SetBit(int idx);
```

Returns a copy of the number with the bit at position `idx` set to `1`.

```csharp
public T UnsetBit(int idx);
```

Returns a copy of the number with the bit at position `idx` set to `0`.

```csharp
public static T FromBits(params bool[] bits);
```

Constructs a number `T` from an array of booleans ordered from least to most significant bit.

## Examples

Reading and modifying bits.

```csharp
byte val = 0b_0000_1010;

val.IsBitSet(0); // false (bit 0 is not set)
val.IsBitSet(1); // true (bit 1 is set)

val = val.SetBit(0).UnsetBit(1);
// 0b_0000_1001
```

Constructing a number from individual bits.

```csharp
var val = byte.FromBits(true, false, true, false, true, true);
// 0b_0011_0101
```
