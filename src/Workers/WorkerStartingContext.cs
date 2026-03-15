using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public sealed class WorkerStartingContext
{
    readonly Action complete;

    internal WorkerStartingContext(Action complete, Task completedEvt, CancellationToken cancellationToken)
    {
        this.complete = complete;
        Completed = completedEvt;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Immediately transitions the worker to the started state.
    /// Calling multiple times, or calling outside of the context of <c>OnStartingAsync</c>, will be a no-op.
    /// </summary>
    public void Complete() => complete();

    /// <summary>
    /// A task which completes when the worker has started. Will be cancelled if the worker fails to start.
    /// Enables worker implementations to set up continuations on themselves.
    /// </summary>
    public Task Completed { get; }

    public CancellationToken CancellationToken { get; }
}