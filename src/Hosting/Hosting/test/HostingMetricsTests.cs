// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Testing.Metering;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingMetricsTests
{
    [Fact]
    public void MultipleRequests()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();
        var meter = meterFactory.Meters.Single();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http-server-request-duration");
        using var currentRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http-server-current-requests");
        using var unhandledRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http-server-unhandled-requests");

        // Act/Assert
        Assert.Equal(HostingMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        // Request 1 (after success)
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context1 = hostingApplication.CreateContext(httpContext.Features);
        context1.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        hostingApplication.DisposeContext(context1, null);

        Assert.Collection(currentRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK));

        // Request 2 (after failure)
        httpContext.Request.Protocol = HttpProtocol.Http2;
        var context2 = hostingApplication.CreateContext(httpContext.Features);
        context2.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        hostingApplication.DisposeContext(context2, new InvalidOperationException("Test error"));

        Assert.Collection(currentRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        // Request 3
        httpContext.Request.Protocol = HttpProtocol.Http3;
        var context3 = hostingApplication.CreateContext(httpContext.Features);
        context3.HttpContext.Items["__RequestUnhandled"] = true;
        context3.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        Assert.Collection(currentRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        hostingApplication.DisposeContext(context3, null);

        Assert.Collection(currentRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"),
            m => AssertRequestDuration(m, HttpProtocol.Http3, StatusCodes.Status404NotFound));
        Assert.Collection(unhandledRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value));

        static void AssertRequestDuration(CollectedMeasurement<double> measurement, string protocol, int statusCode, string exceptionName = null)
        {
            Assert.True(measurement.Value > 0);
            Assert.Equal(protocol, (string)measurement.Tags["protocol"]);
            Assert.Equal(statusCode, (int)measurement.Tags["status-code"]);
            if (exceptionName == null)
            {
                Assert.DoesNotContain(measurement.Tags.ToArray(), t => t.Key == "exception-name");
            }
            else
            {
                Assert.Equal(exceptionName, (string)measurement.Tags.ToArray().Single(t => t.Key == "exception-name").Value);
            }
        }
    }

    [Fact]
    public async Task StartListeningDuringRequest_NotMeasured()
    {
        // Arrange
        var syncPoint = new SyncPoint();
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory, requestDelegate: async ctx =>
        {
            await syncPoint.WaitToContinue();
        });
        var httpContext = new DefaultHttpContext();
        var meter = meterFactory.Meters.Single();

        // Act/Assert
        Assert.Equal(HostingMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        // Request 1 (after success)
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context1 = hostingApplication.CreateContext(httpContext.Features);
        var processRequestTask = hostingApplication.ProcessRequestAsync(context1);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http-server-request-duration");
        using var currentRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http-server-current-requests");
        context1.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

        syncPoint.Continue();
        await processRequestTask.DefaultTimeout();

        hostingApplication.DisposeContext(context1, null);

        Assert.Empty(currentRequestsCollector.GetMeasurementSnapshot());
        Assert.Empty(requestDurationCollector.GetMeasurementSnapshot());
    }

    [Fact]
    public void IHttpMetricsTagsFeatureNotUsedFromFeatureCollection()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();
        var meter = meterFactory.Meters.Single();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http-server-request-duration");
        using var currentRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http-server-current-requests");

        // Act/Assert
        Assert.Equal(HostingMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        // This feature will be overidden by hosting. Hosting is the owner of the feature and is resposible for setting it.
        var overridenFeature = new TestHttpMetricsTagsFeature();
        httpContext.Features.Set<IHttpMetricsTagsFeature>(overridenFeature);

        var context = hostingApplication.CreateContext(httpContext.Features);
        var contextFeature = httpContext.Features.Get<IHttpMetricsTagsFeature>();

        Assert.NotNull(contextFeature);
        Assert.NotEqual(overridenFeature, contextFeature);
    }

    private sealed class TestHttpMetricsTagsFeature : IHttpMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object>> Tags { get; } = new Collection<KeyValuePair<string, object>>();
    }

    private static HostingApplication CreateApplication(IHttpContextFactory httpContextFactory = null, bool useHttpContextAccessor = false,
        ActivitySource activitySource = null, IMeterFactory meterFactory = null, RequestDelegate requestDelegate = null)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        if (useHttpContextAccessor)
        {
            services.AddHttpContextAccessor();
        }

        httpContextFactory ??= new DefaultHttpContextFactory(services.BuildServiceProvider());
        requestDelegate ??= ctx => Task.CompletedTask;

        var hostingApplication = new HostingApplication(
            requestDelegate,
            NullLogger.Instance,
            new DiagnosticListener("Microsoft.AspNetCore"),
            activitySource ?? new ActivitySource("Microsoft.AspNetCore"),
            DistributedContextPropagator.CreateDefaultPropagator(),
            httpContextFactory,
            HostingEventSource.Log,
            new HostingMetrics(meterFactory ?? new TestMeterFactory()));

        return hostingApplication;
    }
}
