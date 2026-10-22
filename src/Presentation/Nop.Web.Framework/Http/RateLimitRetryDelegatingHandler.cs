using System.Net;

namespace Nop.Web.Framework.Http;

/// <summary>
/// Waits before an outbound HTTP 429 retry
/// </summary>
public interface IRateLimitRetryDelay
{
    /// <summary>
    /// Waits for the given delay
    /// </summary>
    /// <param name="delay">Delay</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>
/// Waits with <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
/// </summary>
public class RateLimitRetryDelay : IRateLimitRetryDelay
{
    /// <summary>
    /// Waits for the given delay
    /// </summary>
    /// <param name="delay">Delay</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
            return Task.CompletedTask;

        return Task.Delay(delay, cancellationToken);
    }
}

/// <summary>
/// Retries outbound HTTP calls that return 429 Too Many Requests.
/// Registered once for every client created by <see cref="System.Net.Http.IHttpClientFactory"/>.
/// </summary>
public class RateLimitRetryDelegatingHandler : DelegatingHandler
{
    #region Fields

    private readonly IRateLimitRetryDelay _delay;
    private readonly IRateLimitEventLogger _eventLogger;
    private readonly Func<double> _nextRandom;

    #endregion

    #region Ctor

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="eventLogger">Rate-limit logger</param>
    /// <param name="delay">Wait implementation</param>
    public RateLimitRetryDelegatingHandler(IRateLimitEventLogger eventLogger, IRateLimitRetryDelay delay)
        : this(eventLogger, delay, null)
    {
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="eventLogger">Rate-limit logger</param>
    /// <param name="delay">Wait implementation</param>
    /// <param name="nextRandom">Jitter source from 0 through 1. Pass null to use <see cref="Random.Shared"/></param>
    public RateLimitRetryDelegatingHandler(IRateLimitEventLogger eventLogger, IRateLimitRetryDelay delay, Func<double> nextRandom)
    {
        _eventLogger = eventLogger;
        _delay = delay;
        _nextRandom = nextRandom ?? Random.Shared.NextDouble;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Sends the request and retries HTTP 429 responses with exponential backoff
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final response</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentBytes = request.Content == null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentHeaders = CopyHeaders(request.Content?.Headers);

        HttpRequestMessage attemptRequest = null;
        HttpResponseMessage response = null;
        var logged = false;
        var accumulatedDelay = TimeSpan.Zero;

        try
        {
            for (var attempt = 1; attempt <= RateLimitRetryDefaults.MaxAttempts; attempt++)
            {
                response?.Dispose();
                response = null;
                attemptRequest?.Dispose();
                attemptRequest = CloneRequest(request, contentBytes, contentHeaders);

                response = await base.SendAsync(attemptRequest, cancellationToken);
                if (response.StatusCode != HttpStatusCode.TooManyRequests)
                    return response;

                var decision = RateLimitRetryPolicy.Evaluate(attempt, response, DateTimeOffset.UtcNow, _nextRandom);
                if (!logged)
                {
                    logged = true;
                    var endpoint = RateLimitLogMessages.FormatEndpoint(request.Method.Method, request.RequestUri?.ToString());
                    await _eventLogger.LogAsync(endpoint, (int)response.StatusCode, attempt, decision.Delay, cancellationToken);
                }

                if (!decision.ShouldRetry || accumulatedDelay + decision.Delay > RateLimitRetryDefaults.MaxTotalDelay)
                    return response;

                accumulatedDelay += decision.Delay;
                try
                {
                    await _delay.WaitAsync(decision.Delay, cancellationToken);
                }
                catch
                {
                    response.Dispose();
                    response = null;
                    throw;
                }
            }

            return response;
        }
        finally
        {
            if (response == null)
                attemptRequest?.Dispose();
        }
    }

    /// <summary>
    /// Copies header values so a later retry does not depend on the original content remaining open
    /// </summary>
    /// <param name="headers">Headers</param>
    /// <returns>Copied headers</returns>
    private static List<KeyValuePair<string, string[]>> CopyHeaders(System.Net.Http.Headers.HttpHeaders headers)
    {
        if (headers == null)
            return null;

        return headers
            .Select(header => new KeyValuePair<string, string[]>(header.Key, header.Value.ToArray()))
            .ToList();
    }

    /// <summary>
    /// Creates a request that can be sent after the previous attempt
    /// </summary>
    /// <param name="request">Original request</param>
    /// <param name="contentBytes">Buffered body</param>
    /// <param name="contentHeaders">Buffered content headers</param>
    /// <returns>Request copy</returns>
    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[] contentBytes, List<KeyValuePair<string, string[]>> contentHeaders)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        var options = (IDictionary<string, object>)clone.Options;
        foreach (var option in (IEnumerable<KeyValuePair<string, object>>)request.Options)
        {
            if (option.Value != null)
                options[option.Key] = option.Value;
        }

        if (contentBytes == null)
            return clone;

        var content = new ByteArrayContent(contentBytes);
        if (contentHeaders != null)
        {
            foreach (var header in contentHeaders)
            {
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Content = content;
        return clone;
    }

    #endregion
}
