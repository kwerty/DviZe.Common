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

internal sealed class WorkerContext<TWorker> : WorkerContext, IAsyncDisposable where TWorker : Worker
{
    readonly TaskCompletionSource startedEvtSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource stoppedEvtSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource stoppingTokenSrc = new();
    readonly TWorker worker;
    readonly ILogger logger;
    WorkerState state;
    Task starting;
    Task stopping;
    bool closed;

    public WorkerContext(TWorker worker, ILoggerFactory loggerFactory)
    {
        if (Interlocked.Exchange(ref worker.context, this) != null)
        {
            throw new InvalidOperationException("Worker already owned.");
        }

        this.worker = worker;
        logger = loggerFactory.CreateLogger<WorkerContext<TWorker>>();
    }

    public override Lock LockObj { get; } = new();

    public override WorkerState State => state;

    public override CancellationToken StoppingToken => stoppingTokenSrc.Token;

    public override Task Stopped => stoppedEvtSrc.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var completed = false;
        var startingSrc = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (LockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            state = WorkerState.Starting;
            starting = startingSrc.Task;
        }

        try
        {
            await worker.OnStartingAsync(new WorkerStartingContext(Complete, startedEvtSrc.Task, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            lock (LockObj)
            {
                if (completed)
                {
                    logger.LogCritical(ex, "Unhandled exception; OnStartingAsync faulted, but Complete has already been called.");
                    return;
                }

                completed = true;
                state = WorkerState.Stopped;
                _ = stoppingTokenSrc.CancelAsync();
                startedEvtSrc.SetCanceled(CancellationToken.None);
                stoppedEvtSrc.SetCanceled(CancellationToken.None);
                startingSrc.SetResult();

                throw;
            }
        }

        Complete();


        void Complete()
        {
            lock (LockObj)
            {
                if (!completed)
                {
                    completed = true;
                    state = WorkerState.Started;
                    startedEvtSrc.SetResult();
                    startingSrc.SetResult();
                }
            }
        }
    }

    public override bool TryStop(Exception exception = null)
    {
        var stoppingSrc = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (LockObj)
        {
            if (state != WorkerState.Started)
            {
                return false;
            }

            state = WorkerState.Stopping;
            _ = stoppingTokenSrc.CancelAsync();
            stopping = stoppingSrc.Task;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await worker.OnStoppingAsync().ConfigureAwait(false);
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
                stoppingSrc.SetResult();
            }
        });

        return true;
    }

    /// <summary>
    /// Not strictly necessary if the worker never started, failed to start, or has stopped.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref closed, true))
        {
            return;
        }

        while (true)
        {
            Task waitForTask;

            lock (LockObj)
            {
                switch (state)
                {
                    case WorkerState.Inactive:
                        state = WorkerState.Stopped;
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