using Kwerty.DviZe.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Threading;

public sealed class AsyncGate : IAsyncDisposable
{
    readonly Lock lockObj = new();
    readonly TaskCompletionSource tcs = new();
    int userCount;
    bool closed;

    public IDisposable Enter()
    {
        lock (lockObj)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            userCount++;
        }

        return IDisposable.FromCallback(() =>
        {
            lock (lockObj)
            {
                if (--userCount > 0
                    || !closed)
                {
                    return;
                }
            }

            tcs.SetResult();
        });
    }

    public async ValueTask DisposeAsync()
    {
        var didClose = false;

        lock (lockObj)
        {
            if (!closed)
            {
                closed = true;

                if (userCount == 0)
                {
                    didClose = true;
                }
            }
        }

        if (didClose)
        {
            tcs.SetResult();
        }
        
        await tcs.Task.ConfigureAwait(false);
    }
}