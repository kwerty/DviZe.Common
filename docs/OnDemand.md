# OnDemand&lt;TWorker&gt;

A runner which provides access to a shared worker of type `TWorker`. Workers are created, started and stopped on-demand.

## Constructors

The following constructors are available.

```csharp
public OnDemand(ILoggerFactory loggerFactory);
public OnDemand(OnDemandOptions options, ILoggerFactory loggerFactory);
public OnDemand(Func<TWorker> workerFactory, ILoggerFactory loggerFactory);
public OnDemand(OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory);
```

If a `workerFactory` is not provided, `TWorker` **must** have a parameterless constructor.

`loggerFactory` will be used to log any unhandled exceptions that occur when the worker is starting or stopping.

### Release policy

The worker's lifecycle is governed by the `OnDemandReleasePolicy` defined in `OnDemandOptions`:

* **`ReleaseImmediately`** (Default): The worker is stopped and released when it is no longer in use.
* **`ReleaseAfterDelay`**: When the worker is no longer in use, continue to hold it for a specified period (`ReleaseDelay`) before releasing. If `LeaseAsync` is called during this window, the timer is cancelled, and the worker remains active.
* **`NeverRelease`**: The worker is held until `DisposeAsync` is called.

## Members

### LeaseAsync

```csharp
public Task<WorkerLease<TWorker>> LeaseAsync(CancellationToken cancellationToken = default);
```

Provides access to a shared worker. The returned `WorkerLease<TWorker>` includes the worker and a `Releaser` (`IDisposable`).

The `Releaser` **must** be disposed when the worker is no longer in use to trigger the release policy. Otherwise the worker will remain active for the remainder of the runner's lifetime. If the `ReleasePolicy` is set to `NeverRelease`, disposing the releaser is not required.

If the worker factory throws an exception, or if the worker fails to start due to an exception, then the exception is thrown here, and all future calls to `LeaseAsync` will rethrow the same exception.

If a vote to cancel succeeds (see below), `OperationCanceledException` will be thrown, and `LeaseAsync` can be called again to retry.

#### Cancellation

* Cancellation is a vote, it requires every caller to cancel their token.

* If there is even one caller who doesn't, the operation runs to completion (for every caller).

* If the vote succeeds, the cancellation token passed to the worker (via `OnStartingAsync`) will be cancelled.

* The worker is not required to honour cancellation.

### DisposeAsync

```csharp
public ValueTask DisposeAsync();
```

Disposes the runner, and if there is an active worker, it begins the process of stopping that worker. Completes once the worker has stopped.

If a delayed release is pending, it is stopped, and release happens immediately.

If the active worker is still starting, this won't cancel or interrupt it. Once it has finished starting, it will be stopped.

## Examples

Defining the runner.

```csharp
// Keep the worker alive for 5 minutes after the last user leaves.
var options = new OnDemandOptions(OnDemandReleasePolicy.ReleaseAfterDelay, TimeSpan.FromMinutes(5));

// Define a factory that passes dependencies to the worker constructor.
Func<MyWorker> workerFactory = () => new MyWorker(someDependency);

var onDemand = new OnDemand<MyWorker>(options, workerFactory, loggerFactory);
```

Accessing a worker on demand.

```csharp
var (worker, releaser) = await onDemand.LeaseAsync(CancellationToken.None);
using (releaser)
{
    // Use the worker.
}

// Nobody is using the worker, release policy is triggered.
```

Cleaning up.

```csharp
await onDemand.DisposeAsync();
```
