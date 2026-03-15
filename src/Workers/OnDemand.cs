using Kwerty.DviZe.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

/// <summary>
/// Provides access to a shared worker of type <typeparamref name="TWorker"/>, which will be created
/// and started on-demand, then stopped when there are no more users.
/// </summary>
public sealed partial class OnDemand<TWorker> : IWorkerProvider<TWorker>, IAsyncDisposable where TWorker : Worker
{
    readonly Lock lockObj = new();
    readonly OnDemandOptions options;
    readonly Func<TWorker> workerFactory;
    readonly ILoggerFactory loggerFactory;
    readonly Runner<Session> sessionRunner;
    readonly Runner<User> userRunner;
    Session session;
    bool closed;

    // First arg distinguishes from public constructor with the same params.
    OnDemand(object _, OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(workerFactory, nameof(workerFactory));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        this.options = options;
        this.workerFactory = workerFactory;
        this.loggerFactory = loggerFactory;
        sessionRunner = new(loggerFactory);
        userRunner = new(loggerFactory);
    }

    public OnDemand(ILoggerFactory loggerFactory)
        : this(null, OnDemandOptions.Default, GetDefaultWorkerFactory(), loggerFactory)
    {
    }

    public OnDemand(OnDemandOptions options, ILoggerFactory loggerFactory)
        : this(null, options, GetDefaultWorkerFactory(), loggerFactory)
    {
    }

    public OnDemand(Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
        : this(null, OnDemandOptions.Default, WorkerFactoryFromUser(workerFactory), loggerFactory)
    {
    }

    public OnDemand(OnDemandOptions options, Func<TWorker> workerFactory, ILoggerFactory loggerFactory)
        : this(null, options, WorkerFactoryFromUser(workerFactory), loggerFactory)
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
        if (session?.CachedResult != null)
        {
            var worker = await session.CachedResult.ConfigureAwait(false);
            return new WorkerLease<TWorker>(worker, IDisposable.NullDisposable);
        }

        var user = new User(this);
        await userRunner.StartWorkerAsync(user, cancellationToken).ConfigureAwait(false);
        return new WorkerLease<TWorker>(user.Session.Worker, user);
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

            session?.Close();
            session = null;
        }

        await userRunner.DisposeAsync().ConfigureAwait(false);
        await sessionRunner.DisposeAsync().ConfigureAwait(false);
    }

    Session GetSession()
    {
        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            if (session == null
                || session.Closed)
            {
                session = new Session(options, workerFactory, loggerFactory);
                _ = sessionRunner.StartWorkerAsync(session, CancellationToken.None); // Starts synchronously.
            }

            return session;
        }
    }

    static Func<TWorker> GetDefaultWorkerFactory()
    {
        if (typeof(TWorker).GetConstructor(Type.EmptyTypes) == null)
        {
            throw new NotImplementedException($"{typeof(TWorker).Name} must have a parameterless constructor, or a worker factory must be supplied.");
        }

        return Activator.CreateInstance<TWorker>;
    }

    static Func<TWorker> WorkerFactoryFromUser(Func<TWorker> workerFactory)
    {
        return () =>
        {
            return workerFactory() ?? throw new NotImplementedException();
        };
    }

    class SessionClosedException : Exception;
}
