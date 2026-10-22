using System.Net;

namespace Nop.Web.Framework.Http;

/// <summary>
/// Bounds for outbound HTTP 429 retries
/// </summary>
public static class RateLimitRetryDefaults
{
    /// <summary>
    /// Total send attempts, including the first request
    /// </summary>
    public const int MaxAttempts = 4;

    /// <summary>
    /// Delay before the first retry when the response has no Retry-After header
    /// </summary>
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Multiplier applied to <see cref="BaseDelay"/> after each 429
    /// </summary>
    public const double Multiplier = 2d;

    /// <summary>
    /// Longest single wait. A Retry-After above this is not waited out
    /// </summary>
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Longest combined wait for one outbound call
    /// </summary>
    public static readonly TimeSpan MaxTotalDelay = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Describes whether an outbound call that received HTTP 429 should be retried
/// </summary>
public readonly record struct RateLimitRetryDecision(bool ShouldRetry, TimeSpan Delay, bool UsedRetryAfter);

/// <summary>
/// Chooses the wait before retrying an HTTP 429 response
/// </summary>
public static class RateLimitRetryPolicy
{
    /// <summary>
    /// Gets the retry decision for a completed attempt that returned HTTP 429
    /// </summary>
    /// <param name="completedAttempts">How many attempts have already been sent, including this 429</param>
    /// <param name="response">Response that returned HTTP 429</param>
    /// <param name="utcNow">Current UTC time, used when Retry-After is an HTTP date</param>
    /// <param name="nextRandom">
    /// Optional value from 0 through 1 used to jitter exponential delays.
    /// Retry-After is not jittered. Pass null to keep the full exponential delay.
    /// </param>
    /// <returns>Retry decision</returns>
    public static RateLimitRetryDecision Evaluate(int completedAttempts, HttpResponseMessage response, DateTimeOffset utcNow, Func<double> nextRandom = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode != HttpStatusCode.TooManyRequests || completedAttempts >= RateLimitRetryDefaults.MaxAttempts)
            return new RateLimitRetryDecision(false, TimeSpan.Zero, false);

        if (TryGetRetryAfter(response, utcNow, out var retryAfter))
        {
            if (retryAfter < TimeSpan.Zero)
                retryAfter = TimeSpan.Zero;

            // Waiting longer than the cap would pin the caller. Retrying sooner would ignore Retry-After.
            if (retryAfter > RateLimitRetryDefaults.MaxDelay)
                return new RateLimitRetryDecision(false, retryAfter, true);

            return new RateLimitRetryDecision(true, retryAfter, true);
        }

        var multiplier = Math.Pow(RateLimitRetryDefaults.Multiplier, completedAttempts - 1);
        var delay = TimeSpan.FromMilliseconds(RateLimitRetryDefaults.BaseDelay.TotalMilliseconds * multiplier);
        if (delay > RateLimitRetryDefaults.MaxDelay)
            delay = RateLimitRetryDefaults.MaxDelay;

        if (nextRandom != null)
        {
            var random = Math.Clamp(nextRandom(), 0d, 1d);
            // Keep jitter at or below the computed delay so the cap still holds.
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (0.8d + (0.2d * random)));
        }

        return new RateLimitRetryDecision(true, delay, false);
    }

    /// <summary>
    /// Reads a Retry-After delay from the response
    /// </summary>
    /// <param name="response">HTTP response</param>
    /// <param name="utcNow">Current UTC time</param>
    /// <param name="retryAfter">Parsed delay</param>
    /// <returns>True when the header is present</returns>
    public static bool TryGetRetryAfter(HttpResponseMessage response, DateTimeOffset utcNow, out TimeSpan retryAfter)
    {
        ArgumentNullException.ThrowIfNull(response);

        retryAfter = default;
        var header = response.Headers.RetryAfter;
        if (header == null)
            return false;

        if (header.Delta.HasValue)
        {
            retryAfter = header.Delta.Value;
            return true;
        }

        if (header.Date.HasValue)
        {
            retryAfter = header.Date.Value - utcNow;
            return true;
        }

        return false;
    }
}
