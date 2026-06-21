using System;

namespace Kwerty.DviZe.Workers;

public sealed class OnDemandReleasePolicy
{
    public OnDemandReleasePolicy(OnDemandReleasePolicyType type = OnDemandReleasePolicyType.ReleaseImmediately, TimeSpan? delay = null)
    {
        Type = type;

        if (Type == OnDemandReleasePolicyType.ReleaseAfterDelay)
        {
            if (!delay.HasValue
                || delay.Value == TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            Delay = delay.Value;
        }
    }

    public OnDemandReleasePolicyType Type { get; }

    public TimeSpan? Delay { get; }

    public static OnDemandReleasePolicy Default { get; } = new();
}
