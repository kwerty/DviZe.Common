using Kwerty.DviZe.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

partial class OnDemand<TWorker>
{
    sealed class Session : Worker
    {
        readonly AsyncGate initGate = new();
        readonly AsyncLazy<(TWorker, WorkerContext<TWorker>)> initLazy;
        readonly OnDemandOptions options;
        TWorker innerWorker;
        WorkerContext<TWorker> innerWorkerContext;
        int userCount; // Not meaningful when ReleasePolicy == NeverRelease; Access via CachedResult does not count.
        Timer releaseDelayTimer;
        object releaseToken;

        public Session(OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
        {
            this.options = options;

            initLazy = new AsyncLazy<(TWorker, WorkerContext<TWorker>)>(async ct =>
            {
                var worker = workerFactory();
                var workerContext = new WorkerContext<TWorker>(worker, loggerFactory);
                await workerContext.StartAsync(ct).ConfigureAwait(false);
                return (worker, workerContext);
            });
        }

        public bool Closed => Context.StoppingToken.IsCancellationRequested;

        public TWorker Worker => innerWorker;

        public Task<TWorker> CachedResult { get; private set; }

        public async Task JoinAsync(CancellationToken cancellationToken)
        {
            IDisposable gateReleaser;

            lock (Context.LockObj)
            {
                if (Closed)
                {
                    throw new SessionClosedException();
                }

                userCount++;

                gateReleaser = initGate.Enter();

                releaseDelayTimer?.Dispose();
                releaseDelayTimer = null;
                releaseToken = null;
            }

            try
            {
                try
                {
                    (innerWorker, innerWorkerContext) = await initLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    if (options.ReleasePolicy == OnDemandReleasePolicy.NeverRelease)
                    {
                        CachedResult = Task.FromResult(innerWorker);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    CachedResult = Task.FromException<TWorker>(ex);
                    throw;
                }
            }
            catch
            {
                Leave();
                throw;
            }
            finally
            {
                gateReleaser.Dispose();
            }
        }

        public void Leave()
        {
            lock (Context.LockObj)
            {
                if (--userCount > 0
                    || Closed)
                {
                    return;
                }

                // This if statement ensures the Session remains open if the worker never started.
                if (innerWorker != null)
                {
                    if (options.ReleasePolicy == OnDemandReleasePolicy.ReleaseImmediately)
                    {
                        Close();
                    }
                    else if (options.ReleasePolicy == OnDemandReleasePolicy.ReleaseAfterDelay)
                    {
                        releaseToken = new object();
                        releaseDelayTimer = new Timer(Close, releaseToken, options.ReleaseDelay.Value, Timeout.InfiniteTimeSpan);
                    }
                }
            }
        }

        public void Close(object releaseToken = null)
        {
            lock (Context.LockObj)
            {
                if (releaseToken != null
                    && releaseToken != this.releaseToken)
                {
                    return;
                }

                if (Context.TryStop())
                {
                    releaseDelayTimer?.Dispose();
                    releaseDelayTimer = null;
                    releaseToken = null;
                }
            }
        }

        protected internal override async Task OnStoppingAsync()
        {
            await initGate.DisposeAsync().ConfigureAwait(false);

            if (innerWorkerContext != null)
            {
                await innerWorkerContext.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
