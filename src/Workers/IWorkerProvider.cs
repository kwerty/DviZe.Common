using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

public interface IWorkerProvider<TWorker> where TWorker : Worker
{
    bool TryGet(out TWorker worker);

    Task<WorkerLease<TWorker>> LeaseAsync(CancellationToken cancellationToken = default);
}
