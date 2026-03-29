# AsyncLazy&lt;T&gt;

An async implementation of `Lazy<T>`.

## Constructor

```csharp
public AsyncLazy(Func<CancellationToken, Task<T>> valueFactory);
```

Pass your value factory to the constructor.

## Members

### GetValueAsync

```csharp
public Task<T> GetValueAsync(CancellationToken cancellationToken = default);
```

Starts or joins an existing attempt to get a value from the value factory.

If the value factory throws an exception it will be thrown here, and all future calls to `GetValueAsync` will rethrow the same exception.

If a vote to cancel succeeds (see below), `OperationCanceledException` will be thrown, and `GetValueAsync` can be called again to retry.

#### Cancellation

* Cancellation is a vote, it requires every caller to cancel their token.

* If there is even one caller who doesn't, the operation runs to completion (for every caller).

* If the vote succeeds, the cancellation token passed to the value factory will be cancelled.

* The value factory is not required to honour cancellation.

## Examples

The lazy is defined as follows.

```csharp
async Task<int> MyFactory(CancellationToken cancellationToken)
{
    await Task.Delay(10_000, cancellationToken);
    return 67;
}

var lazy = new AsyncLazy<int>(MyFactory);
```

Accessing the value.

```csharp
var val = await lazy.GetValueAsync(CancellationToken.None);
```

A successful vote to cancel.

```csharp
var task1 = lazy.GetValueAsync(new CancellationToken(canceled: true));
var task2 = lazy.GetValueAsync(new CancellationToken(canceled: true));

// Both operations throw OperationCanceledException.
await Task.WhenAll(task1, task2);
```

An unsuccessful vote to cancel.

```csharp
var task1 = lazy.GetValueAsync(new CancellationToken(canceled: true));
var task2 = lazy.GetValueAsync(CancellationToken.None);

// Both operations run to completion.
await Task.WhenAll(task1, task2);
```
