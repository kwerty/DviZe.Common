# AsyncGate

Controls concurrent access to a shared resource like a gate: `Enter` grants user access, while `DisposeAsync` closes the gate and asynchronously waits for all users to leave.

## Members

### Enter

```csharp
public IDisposable Enter();
```

Permits one user to enter, or throws `ObjectDisposedException` if the gate is closed (or in the process of being closed).

Returns an `IDisposable` which **must** be disposed when the user is done. It is recommend that all calls to `Enter` happen in a `using` block (see example below).

### DisposeAsync

```csharp
public ValueTask DisposeAsync();
```

Closes the gate, preventing future access, and asynchronously waits for all existing users to leave.

Once this method returns, it is guaranteed nobody is accessing the shared resource.

⚠️ If a user fails to leave, this will hang forever.

Calling `DisposeAsync` more than once is a no-op.

## Examples

The gate is defined as follows.

```csharp
var gate = new AsyncGate();
```

All access to the shared resource happens inside the gate.

```csharp
using (gate.Enter())
{
    // Access shared resource here.
}
```

When the gate is closed, it is guaranteed nobody is accessing the shared resource.

```csharp
await gate.DisposeAsync();

// It is now guaranteed that nobody is accessing the shared resource.
```
