using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Moq;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Core.Http;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Web.Framework.Http;
using Nop.Web.Framework.Infrastructure.Extensions;
using Nop.Web.Framework.Mvc.Filters;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.Http;

[TestFixture]
public class RateLimitRetryTests
{
    [Test]
    public void EveryFactoryClientGetsTheRetryHandler()
    {
        var services = new ServiceCollection();
        services.AddNopHttpClients();
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

        // The named client also configures a proxy handler, so the retry handler is an extra action.
        monitor.Get(NopHttpDefaults.DefaultHttpClient).HttpMessageHandlerBuilderActions.Count.Should().BeGreaterThan(1);

        // StoreHttpClient does not configure a proxy. Its only builder action comes from the shared default.
        monitor.Get(typeof(StoreHttpClient).Name).HttpMessageHandlerBuilderActions.Should().ContainSingle();
    }

    [Test]
    public void ExponentialDelayDoublesUntilTheCap()
    {
        using var response = TooManyRequests();

        RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow).Delay.Should().Be(TimeSpan.FromSeconds(1));
        RateLimitRetryPolicy.Evaluate(2, response, DateTimeOffset.UtcNow).Delay.Should().Be(TimeSpan.FromSeconds(2));
        RateLimitRetryPolicy.Evaluate(3, response, DateTimeOffset.UtcNow).Delay.Should().Be(TimeSpan.FromSeconds(4));
        RateLimitRetryPolicy.Evaluate(RateLimitRetryDefaults.MaxAttempts, response, DateTimeOffset.UtcNow).ShouldRetry.Should().BeFalse();
    }

    [Test]
    public void JitterStaysWithinTheComputedDelay()
    {
        using var response = TooManyRequests();

        var full = RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow, () => 1d);
        var reduced = RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow, () => 0d);

        full.Delay.Should().Be(TimeSpan.FromSeconds(1));
        reduced.Delay.Should().Be(TimeSpan.FromMilliseconds(800));
    }

    [Test]
    public void RetryAfterDeltaIsHonored()
    {
        using var response = TooManyRequests();
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3));

        var decision = RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow, () => 0d);

        decision.ShouldRetry.Should().BeTrue();
        decision.UsedRetryAfter.Should().BeTrue();
        decision.Delay.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Test]
    public void RetryAfterDateIsHonored()
    {
        var utcNow = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        using var response = TooManyRequests();
        response.Headers.RetryAfter = new RetryConditionHeaderValue(utcNow.AddSeconds(5));

        var decision = RateLimitRetryPolicy.Evaluate(1, response, utcNow);

        decision.ShouldRetry.Should().BeTrue();
        decision.Delay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void RetryAfterBeyondTheCapIsNotWaited()
    {
        using var response = TooManyRequests();
        response.Headers.RetryAfter = new RetryConditionHeaderValue(RateLimitRetryDefaults.MaxDelay + TimeSpan.FromSeconds(1));

        var decision = RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow);

        decision.ShouldRetry.Should().BeFalse();
        decision.UsedRetryAfter.Should().BeTrue();
        decision.Delay.Should().Be(RateLimitRetryDefaults.MaxDelay + TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Non429ResponseIsNotRetried()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        RateLimitRetryPolicy.Evaluate(1, response, DateTimeOffset.UtcNow).ShouldRetry.Should().BeFalse();
    }

    [Test]
    public async Task HandlerRetries429ThenReturnsSuccessAndLogsOnce()
    {
        var inner = new StatusHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, HttpStatusCode.OK);
        var delay = new RecordingDelay();
        var logger = new RecordingRateLimitLogger();
        using var client = CreateClient(inner, delay, logger);

        using var response = await client.GetAsync("https://api.example.test/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Calls.Should().Be(3);
        delay.Delays.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].StatusCode.Should().Be(429);
        logger.Entries[0].Attempt.Should().Be(1);
        logger.Entries[0].Endpoint.Should().Be("GET https://api.example.test/catalog");
    }

    [Test]
    public async Task HandlerDoesNotRetryOtherStatusCodesOrLogThem()
    {
        var inner = new StatusHandler(HttpStatusCode.InternalServerError);
        var delay = new RecordingDelay();
        var logger = new RecordingRateLimitLogger();
        using var client = CreateClient(inner, delay, logger);

        using var response = await client.GetAsync("https://api.example.test/orders");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        inner.Calls.Should().Be(1);
        delay.Delays.Should().BeEmpty();
        logger.Entries.Should().BeEmpty();
    }

    [Test]
    public async Task HandlerStopsWhenRetryAfterExceedsTheCapAndStillLogsOnce()
    {
        var inner = new StatusHandler(HttpStatusCode.TooManyRequests);
        inner.RetryAfter = RateLimitRetryDefaults.MaxDelay + TimeSpan.FromSeconds(5);
        var delay = new RecordingDelay();
        var logger = new RecordingRateLimitLogger();
        using var client = CreateClient(inner, delay, logger);

        using var response = await client.GetAsync("https://api.example.test/rates");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Calls.Should().Be(1);
        delay.Delays.Should().BeEmpty();
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].RetryAfter.Should().Be(inner.RetryAfter);
    }

    [Test]
    public async Task HandlerReplaysTheRequestBodyOnRetry()
    {
        var inner = new BodyHandler();
        var delay = new RecordingDelay();
        using var client = CreateClient(inner, delay, new RecordingRateLimitLogger());

        using var response = await client.PostAsync("https://api.example.test/orders", new StringContent("{\"id\":7}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Bodies.Should().Equal("{\"id\":7}", "{\"id\":7}");
    }

    [Test]
    public async Task HandlerStopsWhenTheCombinedWaitWouldExceedTheBudget()
    {
        var inner = new StatusHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, HttpStatusCode.OK);
        inner.RetryAfter = TimeSpan.FromSeconds(10);
        var delay = new RecordingDelay();
        var logger = new RecordingRateLimitLogger();
        using var client = CreateClient(inner, delay, logger);

        using var response = await client.GetAsync("https://api.example.test/budget");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        inner.Calls.Should().Be(2);
        delay.Delays.Should().Equal(TimeSpan.FromSeconds(10));
        logger.Entries.Should().ContainSingle();
    }

    [Test]
    public void LogMessageNamesThe429AndDoesNotImplyAnOutboundAttemptForIncomingRejections()
    {
        var (shortMessage, fullMessage) = RateLimitLogMessages.Format("Catalog/GetProducts GET /api/products", 429, attempt: 0, TimeSpan.FromSeconds(1));

        shortMessage.Should().Be("Rate limit (HTTP 429) was hit for Catalog/GetProducts GET /api/products.");
        fullMessage.Should().Contain("Status: 429");
        fullMessage.Should().Contain("Retry-After: 1s");
        fullMessage.Should().NotContain("Retry attempt:");
    }

    [Test]
    public void LogMessageIncludesTheOutboundRetryAttempt()
    {
        var (_, fullMessage) = RateLimitLogMessages.Format("GET https://api.example.test/catalog", 429, attempt: 1, TimeSpan.FromSeconds(2));

        fullMessage.Should().Contain("Retry attempt: 1 of 4");
        fullMessage.Should().Contain("Retry-After: 2s");
    }

    [Test]
    public async Task EventLoggerWritesOneErrorToTheSystemLog()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(item => item.IsEnabled(LogLevel.Error)).Returns(true);
        logger.Setup(item => item.InsertLogAsync(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Customer>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger.Object);
        using var provider = services.BuildServiceProvider();
        var eventLogger = new RateLimitEventLogger(provider.GetRequiredService<IServiceScopeFactory>(), () => true);

        await eventLogger.LogAsync("GET https://api.example.test/catalog", 429, 1, TimeSpan.FromSeconds(2));

        logger.Verify(item => item.InsertLogAsync(
            LogLevel.Error,
            "Rate limit (HTTP 429) was hit for GET https://api.example.test/catalog.",
            It.Is<string>(message => message.Contains("Retry attempt: 1 of 4") && message.Contains("Retry-After: 2s")),
            null), Times.Once);
    }

    [Test]
    public async Task EventLoggerSkipsTheWriteWhenErrorLoggingIsDisabled()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(item => item.IsEnabled(LogLevel.Error)).Returns(false);
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger.Object);
        using var provider = services.BuildServiceProvider();
        var eventLogger = new RateLimitEventLogger(provider.GetRequiredService<IServiceScopeFactory>(), () => true);

        await eventLogger.LogAsync("GET /api/products", 429, 0, TimeSpan.FromSeconds(1));

        logger.Verify(item => item.InsertLogAsync(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Customer>()), Times.Never);
    }

    [Test]
    public async Task ResponseFilterSetsRetryAfterAndLogsIncoming429Once()
    {
        var logger = new RecordingRateLimitLogger();
        var filter = new RateLimitResponseFilter(logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/products";
        var routeData = new RouteData();
        routeData.Values["controller"] = "Catalog";
        routeData.Values["action"] = "GetProducts";
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        var context = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new StatusCodeResult(StatusCodes.Status429TooManyRequests), new object());

        var continued = false;
        await filter.OnResultExecutionAsync(context, () =>
        {
            continued = true;
            return Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller));
        });

        continued.Should().BeTrue();
        httpContext.Response.Headers.RetryAfter.ToString().Should().Be("1");
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].StatusCode.Should().Be(429);
        logger.Entries[0].Attempt.Should().Be(0);
        logger.Entries[0].Endpoint.Should().Be("Catalog/GetProducts GET /api/products");
    }

    [Test]
    public async Task ResponseFilterKeepsAnExistingRetryAfterHeader()
    {
        var logger = new RecordingRateLimitLogger();
        var filter = new RateLimitResponseFilter(logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/products";
        httpContext.Response.Headers.RetryAfter = "9";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new StatusCodeResult(StatusCodes.Status429TooManyRequests), new object());

        await filter.OnResultExecutionAsync(context, () =>
            Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller)));

        httpContext.Response.Headers.RetryAfter.ToString().Should().Be("9");
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].RetryAfter.Should().Be(TimeSpan.FromSeconds(9));
    }

    [Test]
    public async Task ResponseFilterLeavesOtherStatusCodesAlone()
    {
        var logger = new RecordingRateLimitLogger();
        var filter = new RateLimitResponseFilter(logger);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), new StatusCodeResult(StatusCodes.Status400BadRequest), new object());

        await filter.OnResultExecutionAsync(context, () =>
            Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller)));

        httpContext.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
        logger.Entries.Should().BeEmpty();
    }

    private static HttpClient CreateClient(HttpMessageHandler inner, RecordingDelay delay, IRateLimitEventLogger logger)
    {
        var handler = new RateLimitRetryDelegatingHandler(logger, delay, () => 1d)
        {
            InnerHandler = inner
        };

        return new HttpClient(handler);
    }

    private static HttpResponseMessage TooManyRequests()
    {
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
    }

    private sealed class RecordingDelay : IRateLimitRetryDelay
    {
        public List<TimeSpan> Delays { get; } = new();

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRateLimitLogger : IRateLimitEventLogger
    {
        public List<(string Endpoint, int StatusCode, int Attempt, TimeSpan? RetryAfter)> Entries { get; } = new();

        public Task LogAsync(string endpoint, int statusCode, int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken = default)
        {
            Entries.Add((endpoint, statusCode, attempt, retryAfter));
            return Task.CompletedTask;
        }
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _codes;

        public StatusHandler(params HttpStatusCode[] codes)
        {
            _codes = new Queue<HttpStatusCode>(codes);
        }

        public int Calls { get; private set; }

        public TimeSpan? RetryAfter { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var code = _codes.Count > 0 ? _codes.Dequeue() : HttpStatusCode.TooManyRequests;
            var response = new HttpResponseMessage(code);
            if (code == HttpStatusCode.TooManyRequests && RetryAfter.HasValue)
                response.Headers.RetryAfter = new RetryConditionHeaderValue(RetryAfter.Value);

            return Task.FromResult(response);
        }
    }

    private sealed class BodyHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            var code = Bodies.Count == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            return new HttpResponseMessage(code);
        }
    }
}
