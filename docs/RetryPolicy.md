# RetryPolicy

Defines the retry behavior for an operation, including attempt limits, delay duration, and backoff strategy.

## Constructor

### `public RetryPolicy(TimeSpan baseDelay, int? maxRetries = null, RetryPolicyBackoffType backoffType = RetryPolicyBackoffType.Constant, TimeSpan? maxDelay = null)`

`baseDelay` is the initial delay between attempts.

`maxRetries` is the maximum number of retries. If `null`, retries are unlimited.

`backoffType` controls how the delay grows. Defaults to `Constant`, with `Linear` and `Exponential` options.

`maxDelay` caps the delay regardless of backoff growth. If `null`, the cap defaults to `TimeSpan.MaxValue`.

## IDelayGenerator

Cast to `IDelayGenerator` to access the retry policy as a consumer.

### `public bool TryNext(int attempt, out TimeSpan delay)`

Returns `true` if the attempt should proceed, with `delay` representing how long to wait beforehand. Returns `false` if the retry limit has been reached.

ℹ️ `attempt` is zero-based. `0` represents the initial attempt, which always returns `true` with a delay of `TimeSpan.Zero`.

## Examples

Defining a retry policy.

```csharp
var retryPolicy = new RetryPolicy(
    baseDelay: TimeSpan.FromMilliseconds(500),
    backoffType: RetryPolicyBackoffType.Exponential,
    maxDelay: TimeSpan.FromSeconds(30)
);
```

Putting the retry policy to use.

```csharp
var delayGenerator = (IDelayGenerator)retryPolicy;
var attempt = 0;

while (true)
{
    try
    {
        await DoWorkAsync();
        break;
    }
    catch (Exception)
    {
        attempt++;

        if (delayGenerator.TryNext(attempt, out var delay))
        {
            await Task.Delay(delay);
            continue;
        }

        throw; // No more retries.
    }
}

// Operation completed successfully.
```
