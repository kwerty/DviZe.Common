using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Threading;

public sealed class AsyncLazy<T>
{
    readonly Lock lockObj = new();
    readonly Func<CancellationToken, Task<T>> valueFactory;
    readonly bool canRetry;
    Request currRequest;

    public AsyncLazy(Func<CancellationToken, Task<T>> valueFactory, bool canRetry = false)
    {
        ArgumentNullException.ThrowIfNull(valueFactory, nameof(valueFactory));

        this.valueFactory = valueFactory;
        this.canRetry = canRetry;
    }

    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Request request;

            lock (lockObj)
            {
                if (currRequest == null
                    || currRequest.Result.IsCanceled
                    || (currRequest.Result.IsFaulted && canRetry))
                {
                    currRequest = new Request(valueFactory);
                }

                request = currRequest;
            }

            using var _ = request.RegisterVotingToken(cancellationToken);

            try
            {
                return await request.WaitForResultAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (request.Result.IsCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Earlier callers voted to cancel, go next.
            }
        }
    }

    sealed class Request(Func<CancellationToken, Task<T>> valueFactory)
    {
        readonly CancellationTokenSource cts = new();
        readonly TaskCompletionSource<T> resultSrc = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool initialized;
        int voteCount; // Represents the number of uncanceled voting tokens.

        public Task<T> Result => resultSrc.Task;

        public IDisposable RegisterVotingToken(CancellationToken votingToken)
        {
            Interlocked.Increment(ref voteCount);

            return votingToken.Register(() =>
            {
                if (Interlocked.Decrement(ref voteCount) == 0)
                {
                    // No-op if already canceled/completed.

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

        public async Task<T> WaitForResultAsync()
        {
            if (!Interlocked.Exchange(ref initialized, true))
            {
                try
                {
                    var result = await valueFactory(cts.Token).ConfigureAwait(false);
                    resultSrc.SetResult(result);
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    resultSrc.SetCanceled(cts.Token);
                }
                catch (Exception ex)
                {
                    resultSrc.SetException(ex);
                }
                finally
                {
                    cts.Dispose();
                }
            }

            return await Result.ConfigureAwait(false);
        }
    }
}
