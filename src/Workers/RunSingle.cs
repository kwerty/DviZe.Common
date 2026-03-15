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
    TWorker worker;
    WorkerContext<TWorker> workerContext;
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
            this.worker = worker;
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
        
        stoppedEventSrc.TrySetCanceled();
    }

    bool IWorkerProvider<TWorker>.TryGet(out TWorker worker)
    {
        lock (lockObj)
        {
            worker = this.worker;
            return worker != null;
        }
    }

    Task<WorkerLease<TWorker>> IWorkerProvider<TWorker>.LeaseAsync(CancellationToken cancellationToken)
    {
        lock (lockObj)
        {
            if (worker == null)
            {
                throw new InvalidOperationException();
            }

            return Task.FromResult(new WorkerLease<TWorker>(worker, IDisposable.NullDisposable));
        }
    }
}
