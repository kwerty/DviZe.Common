# RunSingle&lt;TWorker&gt;

A runner which executes exactly one worker of type `TWorker`.

## Constructor

### `public RunSingle(ILoggerFactory loggerFactory)`

`loggerFactory` will be used to log any unhandled exceptions that occur when the worker is starting or stopping.

## Members

### `public Task StartWorkerAsync(TWorker worker, CancellationToken cancellationToken = default)`

Takes ownership of a worker, then asynchronously starts it.

If the worker's `OnStartingAsync` runs synchronously, then this method returns synchronously.

If the worker's `OnStartingAsync` throws an exception, it will be thrown here.

If the worker fails to start, or is cancelled, `StartWorkerAsync` can be called again with a new worker.

Throws `InvalidOperationException` if the runner is already managing a worker, or is actively starting a worker, or if the provided worker is owned elsewhere.

Throws `ObjectDisposedException` if this runner has been disposed.

### `public IWorkerProvider<TWorker> Provider { get; }`

Access the worker as a consumer.

Simply casts the runner to `IWorkerProvider<TWorker>` (see below).

### `public Task Stopped { get; }`

A task that completes after the worker has been stopped.

If the worker faulted, then the task will be faulted with the exception. Note, `Stopped.Exception` will be an `AggregateException`, instead use `Stopped.Exception.InnerException`.

If the runner is disposed before a worker starts, the task will be cancelled.

### `public ValueTask DisposeAsync()`

Disposes the runner, and if there is an active worker, it begins the process of stopping that worker. Completes once the worker has stopped.

If the active worker is still starting, this won't cancel or interrupt it. Once it has finished starting, it will be stopped.

## IWorkerProvider&lt;TWorker&gt;

Cast the runner to `IWorkerProvider<TWorker>` to access the worker as a consumer.

### `public bool TryGet(out TWorker worker)`

Gets the worker, or returns `false` if no worker has been started.

### `public Task<WorkerLease<TWorker>> LeaseAsync(CancellationToken cancellationToken = default)`

*Simulates* leasing the worker.

Throws `InvalidOperationException` if no worker has been started.

ℹ️ This is not a real lease. The worker's lifecycle is governed by the producing side. Disposing the included `Releaser` is a no-op.

## Examples

First define the runner.

```csharp
var runner = new RunSingle<MyWorker>(loggerFactory);
```

Creating and starting a worker.

```csharp
await runner.StartWorkerAsync(new MyWorker());
```

Accessing the worker as a consumer.

```csharp
// Returns false if no worker started.
if (runner.Provider.TryGet(out var worker))
{
    // Use the worker.
}

// Simulate on-demand access.
// Throws InvalidOperationException if no worker started.
var (worker, releaser) = await runner.Provider.LeaseAsync(CancellationToken.None);
using (releaser)
{
    // Use the worker.
}
```

Handling the `Stopped` event.

```csharp
runner.Stopped.ContinueWith(_ => {
    // Worker stopped
}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
```

Cleaning up.

```csharp
await runner.DisposeAsync();
```
