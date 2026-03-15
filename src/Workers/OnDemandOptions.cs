using System;

namespace Kwerty.DviZe.Workers;

public sealed class OnDemandOptions
{
    public OnDemandOptions(OnDemandReleasePolicy? releaseAction = null, TimeSpan? releaseDelay = null)
    {
        ReleasePolicy = releaseAction ?? OnDemandReleasePolicy.ReleaseImmediately;

        if (ReleasePolicy == OnDemandReleasePolicy.ReleaseAfterDelay)
        {
            if (!releaseDelay.HasValue
                || releaseDelay.Value == TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseDelay));
            }

            ReleaseDelay = releaseDelay.Value;
        }
    }

    public OnDemandReleasePolicy ReleasePolicy { get; }

    public TimeSpan? ReleaseDelay { get; }

    public static OnDemandOptions Default { get; } = new();
}
