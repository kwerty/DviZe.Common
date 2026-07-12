using Kwerty.DviZe.Common;
using Kwerty.DviZe.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

/// <summary>
/// Provides access to a shared worker of type <typeparamref name="TWorker"/>, which will be created
/// and started on-demand, then stopped when there are no more users.
/// </summary>
public sealed class OnDemand<TWorker> : IWorkerProvider<TWorker>, IAsyncDisposable where TWorker : Worker
{
    readonly Lock lockObj = new();
    readonly OnDemandOptions options;
    readonly Func<TWorker> workerFactory;
    readonly ILoggerFactory loggerFactory;
    readonly Runner<Session> sessionRunner;
    Session currSession;
    bool closed;

    public OnDemand(OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(workerFactory, nameof(workerFactory));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        this.options = options;
        this.workerFactory = workerFactory;
        this.loggerFactory = loggerFactory;
        sessionRunner = new Runner<Session>(loggerFactory);
    }

    public OnDemand(OnDemandOptions options, ILoggerFactory loggerFactory)
        : this(options, Activator.CreateInstance<TWorker>, loggerFactory)
    {
        if (typeof(TWorker).GetConstructor(Type.EmptyTypes) == null)
        {
            throw new InvalidOperationException($"{typeof(TWorker).Name} must have a parameterless constructor, or a worker factory must be supplied.");
        }
    }

    public OnDemand(ILoggerFactory loggerFactory)
        : this(OnDemandOptions.Default, loggerFactory)
    {
    }

    public OnDemand(Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
        : this(OnDemandOptions.Default, workerFactory, loggerFactory)
    {
    }

    bool IWorkerProvider<TWorker>.TryGet(out TWorker worker)
        => throw new NotSupportedException();

    /// <summary>
    /// Provides access to the shared worker. The returned <see cref="WorkerLease{TWorker}"/>
    /// includes a <c>Releaser</c> which should be disposed when the worker is no longer
    /// in use (not required if <c>ReleasePolicy == NeverRelease</c>).
    /// </summary>
    public async Task<WorkerLease<TWorker>> LeaseAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Session session;

            lock (lockObj)
            {
                ObjectDisposedException.ThrowIf(closed, this);

                if (currSession == null
                    || currSession.IsClosed)
                {
                    currSession = new Session(options, workerFactory, loggerFactory);
                    _ = sessionRunner.StartWorkerAsync(currSession, CancellationToken.None); // Completes synchronously.
                }

                session = currSession;
            }

            IDisposable sessionReleaser;

            try
            {
                sessionReleaser = await session.JoinAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SessionClosedException)
            {
                continue;
            }

            return new WorkerLease<TWorker>(session.Worker, sessionReleaser);
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (lockObj)
        {
            if (closed)
            {
                return;
            }

            closed = true;
        }

        await sessionRunner.DisposeAsync().ConfigureAwait(false);
    }

    sealed class Session : Worker
    {
        readonly OnDemandOptions options;
        readonly Func<TWorker> workerFactory;
        readonly ILoggerFactory loggerFactory;
        readonly AsyncGate initGate = new();
        readonly AsyncLazy<WorkerContext<TWorker>> initLazy;
        WorkerContext<TWorker> workerContext;
        int userCount;
        DelayedRelease delayedRelease;

        public Session(OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
        {
            this.options = options;
            this.workerFactory = workerFactory;
            this.loggerFactory = loggerFactory;
            initLazy = new AsyncLazy<WorkerContext<TWorker>>(CreateWorkerContext,
                canRetry: options.RetryPolicy.HasFlag(OnDemandRetryPolicy.RetryAfterWorkerFailedToStart));
        }

        public bool IsClosed => Context.StoppingToken.IsCancellationRequested;

        public TWorker Worker => workerContext?.Worker;

        public async Task<IDisposable> JoinAsync(CancellationToken cancellationToken)
        {
            IDisposable initGateReleaser = null;

            lock (Context.LockObj)
            {
                if (IsClosed)
                {
                    throw new SessionClosedException();
                }

                if (Worker != null)
                {
                    return Join();
                }

                initGateReleaser = initGate.Enter();
            }

            var didInit = false;

            try
            {
                var workerContext = await initLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

                lock (Context.LockObj)
                {
                    didInit = TrySetWorkerContext(workerContext);

                    return Join();
                }
            }
            finally
            {
                initGateReleaser.Dispose();

                if (didInit)
                {
                    await initGate.DisposeAsync().ConfigureAwait(false);

                    OnInitialized();
                }
            }
        }

        IDisposable Join()
        {
            lock (Context.LockObj)
            {
                if (IsClosed)
                {
                    throw new SessionClosedException();
                }

                delayedRelease?.Dispose();
                userCount++;

                return IDisposable.FromCallback(Leave);
            }
        }

        void Leave()
        {
            lock (Context.LockObj)
            {
                if (--userCount > 0
                    || IsClosed)
                {
                    return;
                }

                if (options.ReleasePolicy.Type == OnDemandReleasePolicyType.ReleaseImmediately)
                {
                    Context.TryStop();
                }
                else if (options.ReleasePolicy.Type == OnDemandReleasePolicyType.ReleaseAfterDelay)
                {
                    delayedRelease = new DelayedRelease(Context, options.ReleasePolicy.Delay.Value);
                }
            }
        }

        bool TrySetWorkerContext(WorkerContext<TWorker> workerContext)
        {
            lock (Context.LockObj)
            {
                if (this.workerContext != null)
                {
                    return false;
                }

                this.workerContext = workerContext;
                return true;
            }
        }

        void OnInitialized()
        {
            if (options.RetryPolicy.HasFlag(OnDemandRetryPolicy.RetryAfterWorkerStopped))
            {
                _ = workerContext.Stopped.ContinueWith(_ => Context.TryStop(),
                    Context.StoppingToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }

            if (options.RetryPolicy.HasFlag(OnDemandRetryPolicy.RetryAfterWorkerFaulted))
            {
                _ = workerContext.Stopped.ContinueWith(_ => Context.TryStop(),
                    Context.StoppingToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }

        protected internal override async Task OnStoppingAsync()
        {
            await initGate.DisposeAsync().ConfigureAwait(false);

            if (workerContext != null)
            {
                await workerContext.DisposeAsync().ConfigureAwait(false);
            }
        }

        async Task<WorkerContext<TWorker>> CreateWorkerContext(CancellationToken cancellationToken)
        {
            var worker = workerFactory()
                ?? throw new InvalidOperationException("Worker factory returned null.");

            var workerContext = new WorkerContext<TWorker>(worker, loggerFactory);
            await workerContext.StartAsync(cancellationToken).ConfigureAwait(false);
            return workerContext;
        }
    }

    sealed class DelayedRelease : IDisposable
    {
        readonly CancellationTokenSource cts;

        public DelayedRelease(WorkerContext sessionContext, TimeSpan delay)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(sessionContext.StoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);

                    lock (sessionContext.LockObj)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        sessionContext.TryStop();
                    }
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }

        public void Dispose()
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    sealed class SessionClosedException : Exception;
}
