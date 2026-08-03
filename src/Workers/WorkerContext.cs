using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public abstract class WorkerContext
{
    internal WorkerContext()
    {
    }

    public abstract Lock LockObj { get; }

    public abstract WorkerState State { get; }

    public abstract CancellationToken StoppingToken { get; }

    public abstract Task Stopped { get; }

    public abstract bool TryStop(Exception exception = null);
}

public sealed class WorkerContext<TWorker> : WorkerContext, IAsyncDisposable where TWorker : Worker
{
    readonly TaskCompletionSource startedEvtSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource stoppedEvtSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource stoppingTokenSrc = new();
    readonly ILogger logger;
    WorkerState state;
    Task starting;
    Task stopping;
    bool closed;

    public WorkerContext(TWorker worker, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (Interlocked.Exchange(ref worker.context, this) != null)
        {
            throw new InvalidOperationException("Worker already owned.");
        }

        Worker = worker;
        logger = loggerFactory.CreateLogger<WorkerContext<TWorker>>();
    }

    public override Lock LockObj { get; } = new();

    public TWorker Worker { get; }

    public override WorkerState State => state;

    public override CancellationToken StoppingToken => stoppingTokenSrc.Token;

    public override Task Stopped => stoppedEvtSrc.Task;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var doneSrc = new TaskCompletionSource();
        var resultSrc = new TaskCompletionSource();

        lock (LockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            if (state != WorkerState.Inactive)
            {
                throw new InvalidOperationException();
            }

            state = WorkerState.Starting;
            starting = doneSrc.Task;
        }

        try
        {
            try
            {
                await Worker.OnStartingAsync(new WorkerStartingContext(Complete, resultSrc.Task, cancellationToken)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lock (LockObj)
                {
                    if (resultSrc.Task.IsCompleted)
                    {
                        logger.LogCritical(ex, $"Unhandled exception; OnStartingAsync faulted, but {nameof(WorkerStartingContext.Complete)} has already been called.");
                        return;
                    }

                    if (ex is OperationCanceledException)
                    {
                        Complete(cancel: true);
                    }
                    else
                    {
                        Complete(ex);
                    }

                    throw;
                }
            }

            Complete();

            await resultSrc.Task.ConfigureAwait(false);
        }
        finally
        {
            doneSrc.SetResult();
        }

        void Complete(Exception exception = null, bool cancel = false)
        {
            lock (LockObj)
            {
                if (resultSrc.Task.IsCompleted)
                {
                    return;
                }

                if (exception != null
                    || cancel)
                {
                    state = WorkerState.Stopped;
                    _ = stoppingTokenSrc.CancelAsync();
                    startedEvtSrc.SetCanceled(CancellationToken.None);
                    stoppedEvtSrc.SetCanceled(CancellationToken.None);

                    if (exception != null)
                    {
                        resultSrc.SetException(exception);
                    }
                    else if (cancel)
                    {
                        resultSrc.SetCanceled(CancellationToken.None);
                    }
                    return;
                }

                state = WorkerState.Started;
                startedEvtSrc.SetResult();

                resultSrc.SetResult();
            }
        }
    }

    public override bool TryStop(Exception exception = null)
    {
        var doneSrc = new TaskCompletionSource();

        lock (LockObj)
        {
            if (state == WorkerState.Inactive)
            {
                state = WorkerState.Stopped;
                _ = stoppingTokenSrc.CancelAsync();
                startedEvtSrc.SetCanceled(CancellationToken.None);
                stoppedEvtSrc.SetCanceled(CancellationToken.None);
                return true;
            }

            if (state != WorkerState.Started)
            {
                return false;
            }

            state = WorkerState.Stopping;
            _ = stoppingTokenSrc.CancelAsync();
            stopping = doneSrc.Task;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Worker.OnStoppingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Unhandled exception; OnStoppingAsync faulted.");
            }

            lock (LockObj)
            {
                state = WorkerState.Stopped;
                if (exception == null)
                {
                    stoppedEvtSrc.SetResult();
                }
                else
                {
                    stoppedEvtSrc.SetException(exception);
                }
                doneSrc.SetResult();
            }
        });

        return true;
    }

    /// <summary>
    /// Not strictly necessary if the worker never started, failed to start, or has stopped.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        lock (LockObj)
        {
            if (closed)
            {
                return;
            }

            closed = true;
        }

        while (true)
        {
            Task waitForTask;

            lock (LockObj)
            {
                switch (state)
                {
                    case WorkerState.Inactive:
                        TryStop();
                        return;

                    case WorkerState.Starting:
                        waitForTask = starting;
                        break;

                    case WorkerState.Started:
                        TryStop();
                        waitForTask = stopping;
                        break;

                    case WorkerState.Stopping:
                        waitForTask = stopping;
                        break;

                    case WorkerState.Stopped:
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }

            await waitForTask.ConfigureAwait(false);
        }
    }
}