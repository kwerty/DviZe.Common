# Worker

Worker is an abstract base class. It represents a background work item, and is intended to be executed by a runner such as `Runner<TWorker>`, `RunSingle<TWorker>` or `OnDemand<TWorker>`.

## Members

### `protected virtual Task OnStartingAsync(WorkerStartingContext startingContext)`

Implementations may override this to define initialisation logic.

An implementation may choose to support cancellation by observing `startingContext.CancellationToken`.

If the implementation throws, the exception will be handled by the runner and propagated to the user. The worker will skip to the stopped state.

Calling `base.OnStartingAsync` is a no-op.

### `protected virtual Task OnStoppingAsync()`

Implementations may override this to define teardown logic.

⚠️ Implementations **must not** throw. This is treated as an unhandled exception, and the runner will log a critical message.

This method is not called if the worker fails to start.

Calling `base.OnStoppingAsync` is a no-op.

### `protected WorkerContext Context { get; }`

Provides the worker with access to its own state.

`Context.StoppingToken` provides a cancellation token that is triggered when the worker begins stopping. The worker should observe this token in any background tasks it launches.

ℹ️ Please note that `Context.StoppingToken` is not the same as `startingContext.CancellationToken`. `Context.StoppingToken` is intended to be used **after** the worker starts, where as `startingContext.CancellationToken` is used **before** it starts.

## Example

```csharp
public class MyWorker : Worker
{
    Task backgroundTask;

    protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
    {
        // Simulate async init
        await Task.Delay(50, startingContext.CancellationToken);

        backgroundTask = Task.Run(RunBackgroundTaskAsync, CancellationToken.None);
    }

    async Task RunBackgroundTaskAsync()
    {
        try
        {
            while (true)
            {
                // Do some work.

                // Then sleep for five seconds and repeat, unless we're told to stop.
                await Task.Delay(5000, Context.StoppingToken);
            }
        }
        catch (OperationCanceledException) when (Context.StoppingToken.IsCancellationRequested)
        {
            return;
        }
    }

    protected override async Task OnStoppingAsync()
    {
        // Wait for the background task to stop (it is monitoring Context.StoppingToken)
        await backgroundTask;

        // Simulate async teardown
        await Task.Delay(50, CancellationToken.None);
    }
}
```

## Self-stopping and faulting

After a worker has started it can stop itself by calling `Context.TryStop`. This will succeed and return `true` if the worker is in a valid state to be stopped. Stopping happens asynchronously in the background.

```csharp
Context.TryStop();
```

An exception can be passed to fault the worker. The exception is intended to be observed by the runner. A worker should only fault if it is known that it is being used with a runner that observes the exception. For example, `RunSingle<TWorker>` makes the exception available via `Stopped`.

```csharp
Context.TryStop(new InvalidOperationException());
```

## Advanced usage

`WorkerStartingContext` provides an optional `Complete` method which immediately transitions the worker to the started state.

```csharp
protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
{
    // If we were to register this event here, and it fired right away, then the call to TryStop would be a no-op.
    // someEvent.Register(() => Context.TryStop());

    // But if we call Complete before registering for the event...
    startingContext.Complete();

    // Context.TryStop will succeed because the worker is in the started state.
    someEvent.Register(() => Context.TryStop());
}
```

Under normal circumstances (where `Complete` is not called) `OnStoppingAsync` would only execute **after** `OnStartingAsync` completed. However, once `Complete` is called, `OnStoppingAsync` can execute at any time, so we lose that guarantee.

Calling `Complete` multiple times is a no-op.

⚠️ If `OnStartingAsync` throws after `Complete` has been called, it will be treated as an unhandled exception, and the runner will log a critical message.

`WorkerStartingContext` also provides a `Completed` task which completes when the worker transitions to the started state, or is cancelled if the worker fails to start.

```csharp
protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
{
    // Set up a continuation which only runs if this worker starts.
    backgroundTask = startingContext.Completed.ContinueWith(_ => RunBackgroundTaskAsync(), 
        CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

    // Do some other work, and if theres an exception, the continuation is automatically cancelled.
}
```
