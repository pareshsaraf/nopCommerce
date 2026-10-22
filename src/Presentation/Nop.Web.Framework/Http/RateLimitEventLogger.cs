using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Services.Logging;

namespace Nop.Web.Framework.Http;

/// <summary>
/// Writes one rate-limit event to the nopCommerce system log
/// </summary>
public interface IRateLimitEventLogger
{
    /// <summary>
    /// Logs a rate-limit event at error level so it shows on the admin system log
    /// </summary>
    /// <param name="endpoint">Controller or outbound URL that was limited</param>
    /// <param name="statusCode">HTTP status returned for the limited call</param>
    /// <param name="attempt">Retry attempt that observed the limit. Pass 0 when this is not an outbound retry</param>
    /// <param name="retryAfter">Delay honored or advertised, when known</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task LogAsync(string endpoint, int statusCode, int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the admin log text for a rate-limit event
/// </summary>
public static class RateLimitLogMessages
{
    /// <summary>
    /// Builds a short endpoint description an admin can recognize
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="path">Request path or absolute URL</param>
    /// <param name="controller">Controller name, when the limited call is an incoming request</param>
    /// <param name="action">Action name, when the limited call is an incoming request</param>
    /// <returns>Endpoint text</returns>
    public static string FormatEndpoint(string method, string path, string controller = null, string action = null)
    {
        var requestText = $"{method} {path}".Trim();
        if (string.IsNullOrEmpty(controller) && string.IsNullOrEmpty(action))
            return string.IsNullOrEmpty(requestText) ? "unknown" : Sanitize(requestText);

        return Sanitize($"{controller}/{action} {requestText}".Trim());
    }

    /// <summary>
    /// Builds the system-log messages for one rate-limit event
    /// </summary>
    /// <param name="endpoint">Endpoint text</param>
    /// <param name="statusCode">HTTP status</param>
    /// <param name="attempt">Retry attempt, or 0 when there is no outbound retry</param>
    /// <param name="retryAfter">Retry delay, when known</param>
    /// <returns>Short message and full message</returns>
    public static (string ShortMessage, string FullMessage) Format(string endpoint, int statusCode, int attempt, TimeSpan? retryAfter)
    {
        endpoint = string.IsNullOrWhiteSpace(endpoint) ? "unknown" : Sanitize(endpoint);
        var shortMessage = $"Rate limit (HTTP {statusCode}) was hit for {endpoint}.";

        var fullMessage = new StringBuilder();
        fullMessage.Append("Status: ").Append(statusCode).AppendLine();
        fullMessage.Append("Endpoint: ").Append(endpoint).AppendLine();
        fullMessage.Append("Retry-After: ").Append(FormatDelay(retryAfter));
        if (attempt > 0)
        {
            fullMessage.AppendLine();
            fullMessage.Append("Retry attempt: ").Append(attempt).Append(" of ").Append(RateLimitRetryDefaults.MaxAttempts);
        }

        return (shortMessage, fullMessage.ToString());
    }

    private static string FormatDelay(TimeSpan? retryAfter)
    {
        if (!retryAfter.HasValue)
            return "not specified";

        var seconds = Math.Max(0d, retryAfter.Value.TotalSeconds);
        return string.Create(CultureInfo.InvariantCulture, $"{seconds:0.###}s");
    }

    private static string Sanitize(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ');
    }
}

/// <summary>
/// Logs a rate-limit event through <see cref="ILogger"/> so it appears on System &gt; Log
/// </summary>
public class RateLimitEventLogger : IRateLimitEventLogger
{
    #region Fields

    private readonly Func<bool> _isDatabaseInstalled;
    private readonly IServiceScopeFactory _scopeFactory;

    #endregion

    #region Ctor

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="scopeFactory">Scope factory used so logging does not capture a long-lived database context</param>
    public RateLimitEventLogger(IServiceScopeFactory scopeFactory)
        : this(scopeFactory, null)
    {
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="scopeFactory">Scope factory</param>
    /// <param name="isDatabaseInstalled">Database check. Pass null to use the application data settings</param>
    public RateLimitEventLogger(IServiceScopeFactory scopeFactory, Func<bool> isDatabaseInstalled)
    {
        _scopeFactory = scopeFactory;
        _isDatabaseInstalled = isDatabaseInstalled ?? (() => DataSettingsManager.IsDatabaseInstalled());
    }

    #endregion

    #region Methods

    /// <summary>
    /// Logs one rate-limit event at error level
    /// </summary>
    /// <param name="endpoint">Controller or outbound URL that was limited</param>
    /// <param name="statusCode">HTTP status returned for the limited call</param>
    /// <param name="attempt">Retry attempt that observed the limit. Pass 0 when this is not an outbound retry</param>
    /// <param name="retryAfter">Delay honored or advertised, when known</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task LogAsync(string endpoint, int statusCode, int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken = default)
    {
        if (!_isDatabaseInstalled())
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetService<ILogger>();
            if (logger == null || !logger.IsEnabled(LogLevel.Error))
                return;

            // Do not resolve IWorkContext.GetCurrentCustomerAsync(). With no customer cookie it
            // inserts a guest and rewrites the response cookie. That includes requests the rate
            // limiter has already rejected and outbound calls retried from a request that has not
            // echoed its cookie yet, so the browser comes back as a different customer.
            var (shortMessage, fullMessage) = RateLimitLogMessages.Format(endpoint, statusCode, attempt, retryAfter);
            await logger.InsertLogAsync(LogLevel.Error, shortMessage, fullMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A logging failure must not turn a rate limit into a different error.
        }
    }

    #endregion
}
