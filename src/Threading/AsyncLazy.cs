using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Threading;

public sealed partial class AsyncLazy<T>
{
    readonly Lock lockObj = new();
    readonly Func<CancellationToken, Task<T>> valueFactory;
    readonly bool canRetry;
    Session session;

    public AsyncLazy(Func<CancellationToken, Task<T>> valueFactory, bool canRetry = false)
    {
        ArgumentNullException.ThrowIfNull(valueFactory, nameof(valueFactory));
        this.valueFactory = valueFactory;
        this.canRetry = canRetry;
    }

    Session GetSession()
    {
        lock (lockObj)
        {
            if (session == null
                || session.Closed)
            {
                session = new Session(valueFactory, canRetry);
            }

            return session;
        }
    }

    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var session = GetSession();

            try
            {
                return await session.GetResultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SessionClosedException)
            {
            }
        }
    }

    sealed class SessionClosedException : Exception;
}