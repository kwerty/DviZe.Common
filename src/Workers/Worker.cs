using System;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public abstract class Worker
{
    internal WorkerContext context;

    protected WorkerContext Context => context ?? throw new InvalidOperationException();

    protected internal virtual Task OnStartingAsync(WorkerStartingContext startingContext)
        => Task.CompletedTask;

    protected internal virtual Task OnStoppingAsync()
        => Task.CompletedTask;
}