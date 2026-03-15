using System;

namespace Kwerty.DviZe.Resilience;

public sealed class RetryPolicy : IDelayGenerator
{
    readonly TimeSpan effectiveMaxDelay;

    RetryPolicy()
    {
        MaxRetries = 0;
    }

    public RetryPolicy(TimeSpan baseDelay, int? maxRetries = null, RetryPolicyBackoffType backoffType = RetryPolicyBackoffType.Constant, TimeSpan? maxDelay = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseDelay, TimeSpan.Zero, nameof(baseDelay));
        if (maxRetries.HasValue
            && maxRetries.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        }
        if (maxDelay.HasValue
            && maxDelay.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay));
        }

        BaseDelay = baseDelay;
        MaxRetries = maxRetries;
        BackoffType = backoffType;
        MaxDelay = maxDelay;
        effectiveMaxDelay = maxDelay ?? TimeSpan.MaxValue;
    }

    public TimeSpan BaseDelay { get; }

    public int? MaxRetries { get; }

    public RetryPolicyBackoffType BackoffType { get; }

    public TimeSpan? MaxDelay { get; }

    bool IDelayGenerator.TryNext(int attempt, out TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attempt, nameof(attempt));

        if (attempt == 0)
        {
            delay = TimeSpan.Zero;
            return true;
        }

        if (MaxRetries.HasValue
            && attempt > MaxRetries)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        var factor = BackoffType switch
        {
            RetryPolicyBackoffType.Constant => 1.0,
            RetryPolicyBackoffType.Linear => attempt,
            RetryPolicyBackoffType.Exponential => Math.Pow(2, attempt - 1),
            _ => throw new NotImplementedException(),
        };

        var delayTicks = BaseDelay.Ticks * factor;

        delay = delayTicks < effectiveMaxDelay.Ticks
            ? TimeSpan.FromTicks((long)delayTicks)
            : effectiveMaxDelay;

        return true;
    }

    public static RetryPolicy None { get; } = new RetryPolicy();
}
