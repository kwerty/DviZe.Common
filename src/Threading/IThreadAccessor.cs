using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kwerty.DviZe.Threading;

public interface IThreadAccessor
{
    static readonly ThreadPoolAwaiterGetter threadPoolAwaiterGetter = new();

    public IAwaiterGetter ThreadPool => threadPoolAwaiterGetter;

    public IAwaiterGetter UIThread { get; }

    public interface IAwaiterGetter
    {
        public IAwaiter GetAwaiter();
    }

    public interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }

        void GetResult() { }
    }

    class ThreadPoolAwaiterGetter : IAwaiterGetter
    {
        public IAwaiter GetAwaiter() => new ThreadPoolAwaiter();
    }

    struct ThreadPoolAwaiter : IAwaiter
    {
        public readonly bool IsCompleted => Thread.CurrentThread.IsThreadPoolThread;

        public readonly void OnCompleted(Action continuation)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ => continuation());
        }

        public readonly void GetResult() { }
    }
}
