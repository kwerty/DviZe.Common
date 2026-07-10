using System;

namespace Kwerty.DviZe.Workers;

[Flags]
public enum OnDemandRetryPolicy
{
    None = 0,
    RetryAfterWorkerFailedToStart = 1 << 0,
    RetryAfterWorkerStopped = 1 << 1,
    RetryAfterWorkerFaulted = 1 << 2,
}
