using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Threading;

public sealed partial class AsyncLazy<T>
{
    readonly Lock lockObj = new();
    readonly Func<CancellationToken, Task<T>> valueFactory;
    Session session;

    public AsyncLazy(Func<CancellationToken, Task<T>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory, nameof(valueFactory));
        this.valueFactory = valueFactory;
    }

    Session GetSession()
    {
        lock (lockObj)
        {
            if (session == null
                || session.Closed)
            {
                session = new Session(valueFactory);
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