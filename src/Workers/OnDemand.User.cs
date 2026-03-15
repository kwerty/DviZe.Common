using System;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Workers;

partial class OnDemand<TWorker>
{
    sealed class User(OnDemand<TWorker> parent) : Worker, IDisposable
    {
        public Session Session { get; private set; }

        protected internal override async Task OnStartingAsync(WorkerStartingContext startingContext)
        {
            while (true)
            {
                Session = parent.GetSession();

                try
                {
                    await Session.JoinAsync(startingContext.CancellationToken).ConfigureAwait(false);
                    break;
                }
                catch (SessionClosedException)
                {
                }
            }
        }

        protected internal override Task OnStoppingAsync()
        {
            Session.Leave();
            return Task.CompletedTask;
        }

        void IDisposable.Dispose() => Context.TryStop();
    }
}