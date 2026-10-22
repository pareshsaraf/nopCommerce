using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
using Nop.Web.Framework.Http;

namespace Nop.Web.Framework.Mvc.Filters;

/// <summary>
/// Adds Retry-After and writes one system-log entry when any controller returns HTTP 429
/// </summary>
public sealed class RateLimitResponseFilter : IAsyncResultFilter
{
    #region Fields

    private readonly IRateLimitEventLogger _eventLogger;

    #endregion

    #region Ctor

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="eventLogger">Rate-limit logger</param>
    public RateLimitResponseFilter(IRateLimitEventLogger eventLogger)
    {
        _eventLogger = eventLogger;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Logs an incoming HTTP 429 once and makes sure the response carries Retry-After
    /// </summary>
    /// <param name="context">Result context</param>
    /// <param name="next">Next filter</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (IsTooManyRequests(context))
        {
            var retryAfter = EnsureRetryAfter(context.HttpContext);
            var endpoint = DescribeEndpoint(context);
            await _eventLogger.LogAsync(endpoint, StatusCodes.Status429TooManyRequests, attempt: 0, retryAfter);
        }

        await next();
    }

    /// <summary>
    /// Whether the action result is HTTP 429
    /// </summary>
    /// <param name="context">Result context</param>
    /// <returns>True when the result status is 429</returns>
    private static bool IsTooManyRequests(ResultExecutingContext context)
    {
        return context.Result is IStatusCodeActionResult statusResult &&
               statusResult.StatusCode == StatusCodes.Status429TooManyRequests;
    }

    /// <summary>
    /// Sets Retry-After when the controller did not already provide one
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>Delay advertised to the caller, or null when an existing header cannot be parsed</returns>
    private static TimeSpan? EnsureRetryAfter(HttpContext httpContext)
    {
        if (httpContext.Response.Headers.TryGetValue(HeaderNames.RetryAfter, out var existing))
            return ParseRetryAfter(existing.ToString());

        var delay = RateLimitRetryDefaults.BaseDelay;
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] =
                ((int)delay.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return delay;
    }

    /// <summary>
    /// Parses a Retry-After header value
    /// </summary>
    /// <param name="value">Header value</param>
    /// <returns>Delay, or null when the value is not a delta or an HTTP date</returns>
    private static TimeSpan? ParseRetryAfter(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            return seconds < 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds);

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return null;
    }

    /// <summary>
    /// Describes the controller action that was limited
    /// </summary>
    /// <param name="context">Result context</param>
    /// <returns>Endpoint text</returns>
    private static string DescribeEndpoint(ResultExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        return RateLimitLogMessages.FormatEndpoint(request.Method, $"{request.Path}{request.QueryString}", controller, action);
    }

    #endregion
}
