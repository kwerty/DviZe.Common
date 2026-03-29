# Runner&lt;TWorker&gt;

A runner which executes and manages multiple workers of type `TWorker`.

## Constructor

```csharp
public Runner(ILoggerFactory loggerFactory);
```

`loggerFactory` will be used to log any unhandled exceptions that occur when a worker is starting or stopping.

## Members

### StartWorkerAsync

```csharp
public Task StartWorkerAsync(TWorker worker, CancellationToken cancellationToken = default);
```

Takes ownership of a worker, then asynchronously starts it.

If the worker's `OnStartingAsync` runs synchronously, then this method returns synchronously.

If the worker's `OnStartingAsync` throws an exception, it will be thrown here.

Throws `InvalidOperationException` if the provided worker is owned elsewhere.

Throws `ObjectDisposedException` if this runner has been disposed.

### DisposeAsync

```csharp
public ValueTask DisposeAsync();
```

Disposes the runner, and begins the process of stopping all workers. Completes once all workers have stopped.

This method will wait for any workers that are still starting, it won't cancel or interrupt them. Once they have finished starting, they will be stopped.

## Examples

First define the runner.

```csharp
var runner = new Runner<MyWorker>(loggerFactory);
```

You can then create and start multiple workers.

```csharp
var worker1 = new MyWorker();
var worker2 = new MyWorker();

await runner.StartWorkerAsync(worker1);
await runner.StartWorkerAsync(worker2);
```

Cleaning up.

```csharp
await runner.DisposeAsync();
```
