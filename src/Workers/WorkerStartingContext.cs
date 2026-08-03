using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public sealed class WorkerStartingContext
{
    readonly Action<Exception> complete;

    internal WorkerStartingContext(Action<Exception> complete, Task completedEvt, CancellationToken cancellationToken)
    {
        this.complete = complete;
        Completed = completedEvt;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Synchronously completes the state transition. No-op if already transitioned.
    /// </summary>
    public void Complete(Exception exception = null) => complete(exception);

    /// <summary>
    /// The result of the state transition.
    /// </summary>
    public Task Completed { get; }

    public CancellationToken CancellationToken { get; }
}