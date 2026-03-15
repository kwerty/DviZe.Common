using System;

namespace Kwerty.DviZe.Workers;

public class WorkerLease<TWorker>(TWorker worker, IDisposable releaser) where TWorker : Worker
{
    public TWorker Worker => worker;

    public IDisposable Releaser => releaser;

    public void Deconstruct(out TWorker value1, out IDisposable value2)
    {
        value1 = worker;
        value2 = releaser;
    }
}
