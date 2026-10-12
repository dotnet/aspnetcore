// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;

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

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");
        using var activeRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http.server.active_requests");

        // Act/Assert
        Assert.Equal(HostingMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        // Request 1 (after success)
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context1 = hostingApplication.CreateContext(httpContext.Features);
        context1.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        hostingApplication.DisposeContext(context1, null);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, "1.1", StatusCodes.Status200OK));

        // Request 2 (after failure)
        httpContext.Request.Protocol = HttpProtocol.Http2;
        var context2 = hostingApplication.CreateContext(httpContext.Features);
        context2.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        hostingApplication.DisposeContext(context2, new InvalidOperationException("Test error"));

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, "1.1", StatusCodes.Status200OK),
            m => AssertRequestDuration(m, "2", StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        // Request 3
        httpContext.Request.Protocol = HttpProtocol.Http3;
        var context3 = hostingApplication.CreateContext(httpContext.Features);
        context3.HttpContext.Items["__RequestUnhandled"] = true;
        context3.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, "1.1", StatusCodes.Status200OK),
            m => AssertRequestDuration(m, "2", StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        hostingApplication.DisposeContext(context3, null);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m => AssertRequestDuration(m, "1.1", StatusCodes.Status200OK),
            m => AssertRequestDuration(m, "2", StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"),
            m => AssertRequestDuration(m, "3", StatusCodes.Status404NotFound, unhandledRequest: true));

        static void AssertRequestDuration(CollectedMeasurement<double> measurement, string httpVersion, int statusCode, string exceptionName = null, bool? unhandledRequest = null)
        {
            Assert.True(measurement.Value > 0);
            Assert.Equal(httpVersion, (string)measurement.Tags[HostingTelemetryHelpers.AttributeNetworkProtocolVersion]);
            Assert.Equal(statusCode, (int)measurement.Tags[HostingTelemetryHelpers.AttributeHttpResponseStatusCode]);
            if (exceptionName == null)
            {
                Assert.False(measurement.Tags.ContainsKey(HostingTelemetryHelpers.AttributeErrorType));
            }
            else
            {
                Assert.Equal(exceptionName, (string)measurement.Tags[HostingTelemetryHelpers.AttributeErrorType]);
            }
            if (unhandledRequest ?? false)
            {
                Assert.True((bool)measurement.Tags["aspnetcore.request.is_unhandled"]);
            }
            else
            {
                Assert.False(measurement.Tags.ContainsKey("aspnetcore.request.is_unhandled"));
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

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");
        using var currentRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http.server.active_requests");
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

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");
        using var currentRequestsCollector = new MetricCollector<long>(meterFactory, HostingMetrics.MeterName, "http.server.active_requests");

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

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(599)]
    public void RequestDuration_ServerErrorStatusCode_ErrorTypeSet(int statusCode)
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context = hostingApplication.CreateContext(httpContext.Features);
        context.HttpContext.Response.StatusCode = statusCode;
        hostingApplication.DisposeContext(context, null);

        // Assert
        var measurements = requestDurationCollector.GetMeasurementSnapshot();
        Assert.Single(measurements);

        var measurement = measurements[0];
        Assert.Equal(statusCode, (int)measurement.Tags[HostingTelemetryHelpers.AttributeHttpResponseStatusCode]);
        Assert.Equal(statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture), (string)measurement.Tags[HostingTelemetryHelpers.AttributeErrorType]);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(301)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(499)]
    public void RequestDuration_NonServerErrorStatusCode_NoErrorType(int statusCode)
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context = hostingApplication.CreateContext(httpContext.Features);
        context.HttpContext.Response.StatusCode = statusCode;
        hostingApplication.DisposeContext(context, null);

        // Assert
        var measurements = requestDurationCollector.GetMeasurementSnapshot();
        Assert.Single(measurements);

        var measurement = measurements[0];
        Assert.Equal(statusCode, (int)measurement.Tags[HostingTelemetryHelpers.AttributeHttpResponseStatusCode]);
        Assert.False(measurement.Tags.ContainsKey(HostingTelemetryHelpers.AttributeErrorType));
    }

    [Fact]
    public void RequestDuration_ExceptionWithServerErrorStatusCode_ExceptionTakesPrecedence()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();

        using var requestDurationCollector = new MetricCollector<double>(meterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context = hostingApplication.CreateContext(httpContext.Features);
        context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        hostingApplication.DisposeContext(context, new InvalidOperationException("Test error"));

        // Assert
        var measurements = requestDurationCollector.GetMeasurementSnapshot();
        Assert.Single(measurements);

        var measurement = measurements[0];
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)measurement.Tags[HostingTelemetryHelpers.AttributeHttpResponseStatusCode]);
        // When there's an exception, it should be used as error.type, not the status code
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags[HostingTelemetryHelpers.AttributeErrorType]);
    }

    private sealed class TestHttpMetricsTagsFeature : IHttpMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object>> Tags { get; } = new Collection<KeyValuePair<string, object>>();
        public bool MetricsDisabled { get; set; }
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
