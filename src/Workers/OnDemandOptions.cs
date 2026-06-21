namespace Kwerty.DviZe.Workers;

public sealed class OnDemandOptions
{
    public bool CanRetry { get; init; }

    public OnDemandReleasePolicy ReleasePolicy
    {
        get;
        init => field = value ?? OnDemandReleasePolicy.Default;
    } = OnDemandReleasePolicy.Default;

    public static OnDemandOptions Default { get; } = new();
}
