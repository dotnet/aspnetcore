// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
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

        using var leaseRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request_lease.duration");
        using var currentLeaseRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.active_request_leases");
        using var currentRequestsQueuedCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.queued_requests");
        using var queuedRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request.time_in_queue");
        using var rateLimitingRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.requests");

        // Act
        await middleware.Invoke(context).DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        Assert.Empty(currentLeaseRequestsCollector.GetMeasurementSnapshot());
        Assert.Empty(leaseRequestDurationCollector.GetMeasurementSnapshot());
        Assert.Empty(currentRequestsQueuedCollector.GetMeasurementSnapshot());
        Assert.Empty(queuedRequestDurationCollector.GetMeasurementSnapshot());
        Assert.Collection(rateLimitingRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("global_limiter", (string)m.Tags["aspnetcore.rate_limiting.result"]);
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

        using var leaseRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request_lease.duration");
        using var currentLeaseRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.active_request_leases");
        using var currentRequestsQueuedCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.queued_requests");
        using var queuedRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request.time_in_queue");
        using var rateLimitingRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.requests");

        // Act
        var middlewareTask = middleware.Invoke(context);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        Assert.Collection(currentLeaseRequestsCollector.GetMeasurementSnapshot(),
            m => AssertCounter(m, 1, null));
        Assert.Empty(leaseRequestDurationCollector.GetMeasurementSnapshot());

        syncPoint.Continue();

        await middlewareTask.DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Collection(currentLeaseRequestsCollector.GetMeasurementSnapshot(),
            m => AssertCounter(m, 1, null),
            m => AssertCounter(m, -1, null));
        Assert.Collection(leaseRequestDurationCollector.GetMeasurementSnapshot(),
            m => AssertDuration(m, null));
        Assert.Empty(currentRequestsQueuedCollector.GetMeasurementSnapshot());
        Assert.Empty(queuedRequestDurationCollector.GetMeasurementSnapshot());
        Assert.Collection(rateLimitingRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal("acquired", (string)m.Tags["aspnetcore.rate_limiting.result"]);
            });
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

        using var leaseRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request_lease.duration");
        using var currentLeaseRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.active_request_leases");
        using var currentRequestsQueuedCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.queued_requests");
        using var queuedRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request.time_in_queue");
        using var rateLimitingRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.requests");

        syncPoint.Continue();

        await middlewareTask.DefaultTimeout();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Empty(currentLeaseRequestsCollector.GetMeasurementSnapshot());
        Assert.Collection(leaseRequestDurationCollector.GetMeasurementSnapshot(),
            m => AssertDuration(m, null));
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

        using var leaseRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request_lease.duration");
        using var currentLeaseRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.active_request_leases");
        using var currentRequestsQueuedCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.queued_requests");
        using var queuedRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request.time_in_queue");
        using var rateLimitingRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.requests");

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
        Assert.Collection(currentRequestsQueuedCollector.GetMeasurementSnapshot(),
            m => AssertCounter(m, 1, "concurrencyPolicy"));
        Assert.Empty(queuedRequestDurationCollector.GetMeasurementSnapshot());

        // Allow both requests to finish.
        syncPoint.Continue();

        await middlewareTask1.DefaultTimeout();
        await middlewareTask2.DefaultTimeout();

        Assert.Collection(currentRequestsQueuedCollector.GetMeasurementSnapshot(),
            m => AssertCounter(m, 1, "concurrencyPolicy"),
            m => AssertCounter(m, -1, "concurrencyPolicy"));
        Assert.Collection(queuedRequestDurationCollector.GetMeasurementSnapshot(),
            m =>
            {
                AssertDuration(m, "concurrencyPolicy");
                Assert.Equal("acquired", (string)m.Tags["aspnetcore.rate_limiting.result"]);
            });
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

        using var leaseRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request_lease.duration");
        using var currentLeaseRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.active_request_leases");
        using var currentRequestsQueuedCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.queued_requests");
        using var queuedRequestDurationCollector = new MetricCollector<double>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.request.time_in_queue");
        using var rateLimitingRequestsCollector = new MetricCollector<long>(meterFactory, RateLimitingMetrics.MeterName, "aspnetcore.rate_limiting.requests");

        Assert.Empty(currentRequestsQueuedCollector.GetMeasurementSnapshot());
        Assert.Empty(queuedRequestDurationCollector.GetMeasurementSnapshot());

        // Allow both requests to finish.
        syncPoint.Continue();

        await middlewareTask1.DefaultTimeout();
        await middlewareTask2.DefaultTimeout();

        Assert.Empty(currentRequestsQueuedCollector.GetMeasurementSnapshot());
        Assert.Collection(queuedRequestDurationCollector.GetMeasurementSnapshot(),
            m => AssertDuration(m, "concurrencyPolicy"));
    }

    private static void AssertCounter(CollectedMeasurement<long> measurement, long value, string policy)
    {
        Assert.Equal(value, measurement.Value);
        AssertTag(measurement.Tags, "aspnetcore.rate_limiting.policy", policy);
    }

    private static void AssertDuration(CollectedMeasurement<double> measurement, string policy)
    {
        Assert.True(measurement.Value > 0);
        AssertTag(measurement.Tags, "aspnetcore.rate_limiting.policy", policy);
    }

    private static void AssertTag<T>(IReadOnlyDictionary<string, object> tags, string tagName, T expected)
    {
        if (expected == null)
        {
            Assert.False(tags.ContainsKey(tagName));
        }
        else
        {
            Assert.Equal(expected, (T)tags[tagName]);
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
