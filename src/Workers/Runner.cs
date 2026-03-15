using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

/// <summary>
/// Executes and manages multiple instances of workers of type <typeparamref name="TWorker"/>.
/// </summary>
public sealed class Runner<TWorker>(ILoggerFactory loggerFactory) : IAsyncDisposable where TWorker : Worker
{
    readonly Lock lockObj = new();
    readonly List<WorkerContext<TWorker>> workerContexts = [];
    bool closed;

    public async Task StartWorkerAsync(TWorker worker, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worker, nameof(worker));

        var workerContext = new WorkerContext<TWorker>(worker, loggerFactory);

        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            workerContexts.Add(workerContext);
        }

        try
        {
            await workerContext.StartAsync(cancellationToken).ConfigureAwait(false); // May throw ObjectDisposedException.
        }
        catch
        {
            RemoveWorkerContext();
            throw;
        }

        _ = workerContext.Stopped.ContinueWith(_ => RemoveWorkerContext(), 
            CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);


        void RemoveWorkerContext()
        {
            lock (lockObj)
            {
                workerContexts.Remove(workerContext); // Disposal unnecessary.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<WorkerContext<TWorker>> workerContexts = null;

        lock (lockObj)
        {
            if (closed)
            {
                return;
            }

            closed = true;

            workerContexts = this.workerContexts.ToList();
        }

        await Parallel.ForEachAsync(workerContexts, (ctx, _) => ctx.DisposeAsync()).ConfigureAwait(false);
    }
}
