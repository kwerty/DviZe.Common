using Kwerty.DviZe.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public sealed class RunSingle<TWorker>(ILoggerFactory loggerFactory) : IWorkerProvider<TWorker>, IAsyncDisposable where TWorker : Worker
{
    readonly Lock lockObj = new();
    readonly TaskCompletionSource stoppedEventSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
    WorkerContext<TWorker> workerContext;
    bool started;
    bool closed;

    public async Task StartWorkerAsync(TWorker worker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worker, nameof(worker));

        var workerContext = new WorkerContext<TWorker>(worker, loggerFactory);

        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            if (this.workerContext != null)
            {
                throw new InvalidOperationException();
            }

            this.workerContext = workerContext;
        }

        try
        {
            await workerContext.StartAsync(cancellationToken).ConfigureAwait(false); // May throw ObjectDisposedException.
        }
        catch
        {
            lock (lockObj)
            {
                this.workerContext = null; // Disposal unnecessary.
            }

            throw;
        }

        lock (lockObj)
        {
            started = true;
        }

        _ = workerContext.Stopped.ContinueWith(stoppedEventSrc.SetFromTask, 
            CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
    }

    public IWorkerProvider<TWorker> Provider => this;

    public Task Stopped => stoppedEventSrc.Task;

    public async ValueTask DisposeAsync()
    {
        WorkerContext<TWorker> workerContext;

        lock (lockObj)
        {
            if (closed)
            {
                return;
            }

            closed = true;

            workerContext = this.workerContext;
        }

        if (workerContext != null)
        {
            await workerContext.DisposeAsync().ConfigureAwait(false);
        }

        if (!started)
        {
            stoppedEventSrc.SetCanceled();
        }
    }

    bool IWorkerProvider<TWorker>.TryGet(out TWorker worker)
    {
        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            if (!started)
            {
                worker = null;
                return false;
            }

            worker = workerContext.Worker;
            return started;
        }
    }

    Task<WorkerLease<TWorker>> IWorkerProvider<TWorker>.LeaseAsync(CancellationToken cancellationToken)
    {
        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            if (!started)
            {
                throw new InvalidOperationException();
            }

            return Task.FromResult(new WorkerLease<TWorker>(workerContext.Worker, IDisposable.NullDisposable));
        }
    }
}
