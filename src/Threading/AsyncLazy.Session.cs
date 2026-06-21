using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Threading;

partial class AsyncLazy<T>
{
    sealed class Session
    {
        readonly Task<T> runTask;
        readonly CancellationTokenSource cts = new();
        int uncancelledCount;
        volatile bool closed;

        public Session(Func<CancellationToken, Task<T>> valueFactory, bool canRetry)
        {
            runTask = Task.Run(async () =>
            {
                try
                {
                    return await valueFactory(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    closed = true;
                    throw;
                }
                catch when (canRetry)
                {
                    closed = true;
                    throw;
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }

        public bool Closed => closed;

        public async Task<T> GetResultAsync(CancellationToken cancellationToken)
        {
            if (closed)
            {
                throw new SessionClosedException();
            }

            IDisposable ctRegistration = null;

            if (!runTask.IsCompleted)
            {
                Interlocked.Increment(ref uncancelledCount);

                ctRegistration = cancellationToken.Register(() =>
                {
                    if (Interlocked.Decrement(ref uncancelledCount) == 0)
                    {
                        try
                        {
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                });
            }

            try
            {
                return await runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                if (ctRegistration == null)
                {
                    throw new SessionClosedException();
                }

                throw; // Could throw a new OCE with the user's CT, but don't really see the point.
            }
            finally
            {
                ctRegistration?.Dispose();
            }
        }
    }
}