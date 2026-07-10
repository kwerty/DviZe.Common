namespace Kwerty.DviZe.Workers;

public sealed class OnDemandOptions
{
    public OnDemandRetryPolicy RetryPolicy { get; init;  } = OnDemandRetryPolicy.None;

    public OnDemandReleasePolicy ReleasePolicy
    {
        get;
        init => field = value ?? OnDemandReleasePolicy.Default;
    } = OnDemandReleasePolicy.Default;

    public static OnDemandOptions Default { get; } = new();
}
