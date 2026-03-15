using System;
using System.Threading;

namespace Kwerty.DviZe.Common;

public static class DisposableExtensions
{
    static readonly NullDisposable nullDisposable = new();

    extension(IDisposable)
    {
        public static IDisposable FromCallback(Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback, nameof(callback));
            return new DisposableCallback(callback);
        }

        public static IDisposable NullDisposable => nullDisposable;
    }

    sealed class DisposableCallback(Action callback) : IDisposable
    {
        bool disposed;

        public void Dispose()
        {
            if (!Interlocked.Exchange(ref disposed, true))
            {
                callback();
            }
        }
    }

    sealed class NullDisposable() : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
