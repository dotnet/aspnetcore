// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingMetricsTests
{
    [Fact]
    public async Task Metrics_Rejected()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();

        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));

        var middleware = CreateTestRateLimitingMiddleware(options, meterFactory: meterFactory);
        var meter = meterFactory.Meters.Single();

        var context = new DefaultHttpContext();

        using var leaseRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "leased-request-duration");
        using var currentLeaseRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-leased-requests");
        using var currentRequestsQueuedRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-queued-requests");
        using var queuedRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "queued-request-duration");
        using var leaseFailedRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "lease-failed-requests");

        // Act
        await middleware.Invoke(context).DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        Assert.Empty(currentLeaseRequestsRecorder.GetMeasurements());
        Assert.Empty(leaseRequestDurationRecorder.GetMeasurements());
        Assert.Empty(currentRequestsQueuedRecorder.GetMeasurements());
        Assert.Empty(queuedRequestDurationRecorder.GetMeasurements());
        Assert.Collection(leaseFailedRequestsRecorder.GetMeasurements(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("GlobalLimiter", (string)m.Tags.ToArray().Single(t => t.Key == "reason").Value);
            });
    }

    [Fact]
    public async Task Metrics_Success()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        var meterFactory = new TestMeterFactory();

        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true));

        var middleware = CreateTestRateLimitingMiddleware(
            options,
            meterFactory: meterFactory,
            next: async c =>
            {
                await syncPoint.WaitToContinue();
            });
        var meter = meterFactory.Meters.Single();

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        using var leaseRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "leased-request-duration");
        using var currentLeaseRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-leased-requests");
        using var currentRequestsQueuedRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-queued-requests");
        using var queuedRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "queued-request-duration");
        using var leaseFailedRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "lease-failed-requests");

        // Act
        var middlewareTask = middleware.Invoke(context);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        Assert.Collection(currentLeaseRequestsRecorder.GetMeasurements(),
            m => AssertCounter(m, 1, null, null, null));
        Assert.Empty(leaseRequestDurationRecorder.GetMeasurements());

        syncPoint.Continue();

        await middlewareTask.DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Collection(currentLeaseRequestsRecorder.GetMeasurements(),
            m => AssertCounter(m, 1, null, null, null),
            m => AssertCounter(m, -1, null, null, null));
        Assert.Collection(leaseRequestDurationRecorder.GetMeasurements(),
            m => AssertDuration(m, null, null, null));
        Assert.Empty(currentRequestsQueuedRecorder.GetMeasurements());
        Assert.Empty(queuedRequestDurationRecorder.GetMeasurements());
        Assert.Empty(leaseFailedRequestsRecorder.GetMeasurements());
    }

    [Fact]
    public async Task Metrics_ListenInMiddleOfRequest_CurrentLeasesNotDecreased()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        var meterFactory = new TestMeterFactory();

        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true));

        var middleware = CreateTestRateLimitingMiddleware(
            options,
            meterFactory: meterFactory,
            next: async c =>
            {
                await syncPoint.WaitToContinue();
            });
        var meter = meterFactory.Meters.Single();

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        // Act
        var middlewareTask = middleware.Invoke(context);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        using var leaseRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "leased-request-duration");
        using var currentLeaseRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-leased-requests");
        using var currentRequestsQueuedRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-queued-requests");
        using var queuedRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "queued-request-duration");
        using var leaseFailedRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "lease-failed-requests");

        syncPoint.Continue();

        await middlewareTask.DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Empty(currentLeaseRequestsRecorder.GetMeasurements());
        Assert.Collection(leaseRequestDurationRecorder.GetMeasurements(),
            m => AssertDuration(m, null, null, null));
    }

    [Fact]
    public async Task Metrics_Queued()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        var meterFactory = new TestMeterFactory();

        var services = new ServiceCollection();

        services.AddRateLimiter(_ => _
            .AddConcurrencyLimiter(policyName: "concurrencyPolicy", options =>
            {
                options.PermitLimit = 1;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 1;
            }));
        var serviceProvider = services.BuildServiceProvider();

        var middleware = CreateTestRateLimitingMiddleware(
            serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>(),
            meterFactory: meterFactory,
            next: async c =>
            {
                await syncPoint.WaitToContinue();
            },
            serviceProvider: serviceProvider);
        var meter = meterFactory.Meters.Single();

        var routeEndpointBuilder = new RouteEndpointBuilder(c => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
        routeEndpointBuilder.Metadata.Add(new EnableRateLimitingAttribute("concurrencyPolicy"));
        var endpoint = routeEndpointBuilder.Build();

        using var leaseRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "leased-request-duration");
        using var currentLeaseRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-leased-requests");
        using var currentRequestsQueuedRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-queued-requests");
        using var queuedRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "queued-request-duration");
        using var leaseFailedRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "lease-failed-requests");

        // Act
        var context1 = new DefaultHttpContext();
        context1.Request.Method = "GET";
        context1.SetEndpoint(endpoint);
        var middlewareTask1 = middleware.Invoke(context1);

        // Wait for first request to reach server and block it.
        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        var context2 = new DefaultHttpContext();
        context2.Request.Method = "GET";
        context2.SetEndpoint(endpoint);
        var middlewareTask2 = middleware.Invoke(context1);

        // Assert second request is queued.
        Assert.Collection(currentRequestsQueuedRecorder.GetMeasurements(),
            m => AssertCounter(m, 1, "GET", "/", "concurrencyPolicy"));
        Assert.Empty(queuedRequestDurationRecorder.GetMeasurements());

        // Allow both requests to finish.
        syncPoint.Continue();

        await middlewareTask1.DefaultTimeout();
        await middlewareTask2.DefaultTimeout();

        Assert.Collection(currentRequestsQueuedRecorder.GetMeasurements(),
            m => AssertCounter(m, 1, "GET", "/", "concurrencyPolicy"),
            m => AssertCounter(m, -1, "GET", "/", "concurrencyPolicy"));
        Assert.Collection(queuedRequestDurationRecorder.GetMeasurements(),
            m => AssertDuration(m, "GET", "/", "concurrencyPolicy"));
    }

    [Fact]
    public async Task Metrics_ListenInMiddleOfQueued_CurrentQueueNotDecreased()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        var meterFactory = new TestMeterFactory();

        var services = new ServiceCollection();

        services.AddRateLimiter(_ => _
            .AddConcurrencyLimiter(policyName: "concurrencyPolicy", options =>
            {
                options.PermitLimit = 1;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 1;
            }));
        var serviceProvider = services.BuildServiceProvider();

        var middleware = CreateTestRateLimitingMiddleware(
            serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>(),
            meterFactory: meterFactory,
            next: async c =>
            {
                await syncPoint.WaitToContinue();
            },
            serviceProvider: serviceProvider);
        var meter = meterFactory.Meters.Single();

        var routeEndpointBuilder = new RouteEndpointBuilder(c => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
        routeEndpointBuilder.Metadata.Add(new EnableRateLimitingAttribute("concurrencyPolicy"));
        var endpoint = routeEndpointBuilder.Build();

        // Act
        var context1 = new DefaultHttpContext();
        context1.Request.Method = "GET";
        context1.SetEndpoint(endpoint);
        var middlewareTask1 = middleware.Invoke(context1);

        // Wait for first request to reach server and block it.
        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        var context2 = new DefaultHttpContext();
        context2.Request.Method = "GET";
        context2.SetEndpoint(endpoint);
        var middlewareTask2 = middleware.Invoke(context1);

        // Start listening while the second request is queued.

        using var leaseRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "leased-request-duration");
        using var currentLeaseRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-leased-requests");
        using var currentRequestsQueuedRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "current-queued-requests");
        using var queuedRequestDurationRecorder = new InstrumentRecorder<double>(meterFactory, RateLimitingMetrics.MeterName, "queued-request-duration");
        using var leaseFailedRequestsRecorder = new InstrumentRecorder<long>(meterFactory, RateLimitingMetrics.MeterName, "lease-failed-requests");

        Assert.Empty(currentRequestsQueuedRecorder.GetMeasurements());
        Assert.Empty(queuedRequestDurationRecorder.GetMeasurements());

        // Allow both requests to finish.
        syncPoint.Continue();

        await middlewareTask1.DefaultTimeout();
        await middlewareTask2.DefaultTimeout();

        Assert.Empty(currentRequestsQueuedRecorder.GetMeasurements());
        Assert.Collection(queuedRequestDurationRecorder.GetMeasurements(),
            m => AssertDuration(m, "GET", "/", "concurrencyPolicy"));
    }

    private static void AssertCounter(Measurement<long> measurement, long value, string method, string route, string policy)
    {
        Assert.Equal(value, measurement.Value);
        AssertTag(measurement.Tags, "method", method);
        AssertTag(measurement.Tags, "route", route);
        AssertTag(measurement.Tags, "policy", policy);
    }

    private static void AssertDuration(Measurement<double> measurement, string method, string route, string policy)
    {
        Assert.True(measurement.Value > 0);
        AssertTag(measurement.Tags, "method", method);
        AssertTag(measurement.Tags, "route", route);
        AssertTag(measurement.Tags, "policy", policy);
    }

    private static void AssertTag<T>(ReadOnlySpan<KeyValuePair<string, object>> tags, string tagName, T expected)
    {
        if (expected == null)
        {
            Assert.DoesNotContain(tags.ToArray(), t => t.Key == tagName);
        }
        else
        {
            Assert.Equal(expected, (T)tags.ToArray().Single(t => t.Key == tagName).Value);
        }
    }

    private RateLimitingMiddleware CreateTestRateLimitingMiddleware(IOptions<RateLimiterOptions> options, ILogger<RateLimitingMiddleware> logger = null, IServiceProvider serviceProvider = null, IMeterFactory meterFactory = null, RequestDelegate next = null)
    {
        next ??= c => Task.CompletedTask;
        return new RateLimitingMiddleware(
            next,
            logger ?? new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            serviceProvider ?? Mock.Of<IServiceProvider>(),
            new RateLimitingMetrics(meterFactory ?? new TestMeterFactory()));
    }

    private IOptions<RateLimiterOptions> CreateOptionsAccessor() => Options.Create(new RateLimiterOptions());
}
