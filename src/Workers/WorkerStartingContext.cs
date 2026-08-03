using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public sealed class WorkerStartingContext
{
    readonly Action<Exception, bool> complete;

    internal WorkerStartingContext(Action<Exception, bool> complete, Task completedEvt, CancellationToken cancellationToken)
    {
        this.complete = complete;
        Completed = completedEvt;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Synchronously completes the state transition. No-op if already transitioned.
    /// </summary>
    public void Complete(bool cancel = false) => complete(null, cancel);

    /// <summary>
    /// Synchronously completes the state transition. No-op if already transitioned.
    /// </summary>
    public void Complete(Exception exception) => complete(exception, false);

    /// <summary>
    /// The result of the state transition.
    /// </summary>
    public Task Completed { get; }

    public CancellationToken CancellationToken { get; }
}