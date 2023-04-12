// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Metrics;
using Moq;
using static Microsoft.AspNetCore.Hosting.HostingApplication;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingApplicationTests
{
    [Fact]
    public void Metrics()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var meterRegistry = new TestMeterRegistry(meterFactory.Meters);
        var hostingApplication = CreateApplication(meterFactory: meterFactory);
        var httpContext = new DefaultHttpContext();
        var meter = meterFactory.Meters.Single();

        using var requestDurationRecorder = new InstrumentRecorder<double>(meterRegistry, HostingMetrics.MeterName, "request-duration");
        using var currentRequestsRecorder = new InstrumentRecorder<long>(meterRegistry, HostingMetrics.MeterName, "current-requests");

        // Act/Assert
        Assert.Equal(HostingMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        // Request 1 (after success)
        httpContext.Request.Protocol = HttpProtocol.Http11;
        var context1 = hostingApplication.CreateContext(httpContext.Features);
        context1.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        hostingApplication.DisposeContext(context1, null);

        Assert.Collection(currentRequestsRecorder.GetMeasurements(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationRecorder.GetMeasurements(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK));

        // Request 2 (after failure)
        httpContext.Request.Protocol = HttpProtocol.Http2;
        var context2 = hostingApplication.CreateContext(httpContext.Features);
        context2.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        hostingApplication.DisposeContext(context2, new InvalidOperationException("Test error"));

        Assert.Collection(currentRequestsRecorder.GetMeasurements(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationRecorder.GetMeasurements(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        // Request 3
        httpContext.Request.Protocol = HttpProtocol.Http3;
        var context3 = hostingApplication.CreateContext(httpContext.Features);
        context3.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

        Assert.Collection(currentRequestsRecorder.GetMeasurements(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value));
        Assert.Collection(requestDurationRecorder.GetMeasurements(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"));

        hostingApplication.DisposeContext(context3, null);

        Assert.Collection(currentRequestsRecorder.GetMeasurements(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationRecorder.GetMeasurements(),
            m => AssertRequestDuration(m, HttpProtocol.Http11, StatusCodes.Status200OK),
            m => AssertRequestDuration(m, HttpProtocol.Http2, StatusCodes.Status500InternalServerError, exceptionName: "System.InvalidOperationException"),
            m => AssertRequestDuration(m, HttpProtocol.Http3, StatusCodes.Status200OK));

        static void AssertRequestDuration(Measurement<double> measurement, string protocol, int statusCode, string exceptionName = null)
        {
            Assert.True(measurement.Value > 0);
            Assert.Equal(protocol, (string)measurement.Tags.ToArray().Single(t => t.Key == "protocol").Value);
            Assert.Equal(statusCode, (int)measurement.Tags.ToArray().Single(t => t.Key == "status-code").Value);
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
    public void DisposeContextDoesNotClearHttpContextIfDefaultHttpContextFactoryUsed()
    {
        // Arrange
        var hostingApplication = CreateApplication();
        var httpContext = new DefaultHttpContext();

        var context = hostingApplication.CreateContext(httpContext.Features);
        Assert.NotNull(context.HttpContext);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
        Assert.NotNull(context.HttpContext);
    }

    [Fact]
    public void DisposeContextClearsHttpContextIfIHttpContextAccessorIsActive()
    {
        // Arrange
        var hostingApplication = CreateApplication(useHttpContextAccessor: true);
        var httpContext = new DefaultHttpContext();

        var context = hostingApplication.CreateContext(httpContext.Features);
        Assert.NotNull(context.HttpContext);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
        Assert.Null(context.HttpContext);
    }

    [Fact]
    public void CreateContextReinitializesPreviouslyStoredDefaultHttpContext()
    {
        // Arrange
        var hostingApplication = CreateApplication();
        var features = new FeaturesWithContext<Context>(new DefaultHttpContext().Features);
        var previousContext = new DefaultHttpContext();
        // Pretend like we had previous HttpContext
        features.HostContext = new Context();
        features.HostContext.HttpContext = previousContext;

        var context = hostingApplication.CreateContext(features);
        Assert.Same(previousContext, context.HttpContext);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
        Assert.Same(previousContext, context.HttpContext);
    }

    [Fact]
    public void CreateContextCreatesNewContextIfNotUsingDefaultHttpContextFactory()
    {
        // Arrange
        var factory = new Mock<IHttpContextFactory>();
        factory.Setup(m => m.Create(It.IsAny<IFeatureCollection>())).Returns<IFeatureCollection>(f => new DefaultHttpContext(f));
        factory.Setup(m => m.Dispose(It.IsAny<HttpContext>())).Callback(() => { });

        var hostingApplication = CreateApplication(factory.Object);
        var features = new FeaturesWithContext<Context>(new DefaultHttpContext().Features);
        var previousContext = new DefaultHttpContext();
        // Pretend like we had previous HttpContext
        features.HostContext = new Context();
        features.HostContext.HttpContext = previousContext;

        var context = hostingApplication.CreateContext(features);
        Assert.NotSame(previousContext, context.HttpContext);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/35142")]
    public void IHttpActivityFeatureIsPopulated()
    {
        var testSource = new ActivitySource(Path.GetRandomFileName());
        var dummySource = new ActivitySource(Path.GetRandomFileName());
        using var listener = new ActivityListener
        {
            ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource) ||
                                                ReferenceEquals(activitySource, dummySource)),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var hostingApplication = CreateApplication(activitySource: testSource);
        var httpContext = new DefaultHttpContext();
        var context = hostingApplication.CreateContext(httpContext.Features);

        var activityFeature = context.HttpContext.Features.Get<IHttpActivityFeature>();
        Assert.NotNull(activityFeature);
        Assert.NotNull(activityFeature.Activity);
        Assert.Equal(HostingApplicationDiagnostics.ActivityName, activityFeature.Activity.DisplayName);
        var initialActivity = Activity.Current;

        // Create nested dummy Activity
        using var _ = dummySource.StartActivity("DummyActivity");

        Assert.Same(initialActivity, activityFeature.Activity);
        Assert.Null(activityFeature.Activity.ParentId);
        Assert.Equal(activityFeature.Activity.Id, Activity.Current.ParentId);
        Assert.NotEqual(Activity.Current, activityFeature.Activity);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
    }

    private class TestHttpActivityFeature : IHttpActivityFeature
    {
        public Activity Activity { get; set; }
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/38736")]
    public void IHttpActivityFeatureIsAssignedToIfItExists()
    {
        var testSource = new ActivitySource(Path.GetRandomFileName());
        var dummySource = new ActivitySource(Path.GetRandomFileName());
        using var listener = new ActivityListener
        {
            ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource) ||
                                                ReferenceEquals(activitySource, dummySource)),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var hostingApplication = CreateApplication(activitySource: testSource);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpActivityFeature>(new TestHttpActivityFeature());
        var context = hostingApplication.CreateContext(httpContext.Features);

        var activityFeature = context.HttpContext.Features.Get<IHttpActivityFeature>();
        Assert.NotNull(activityFeature);
        Assert.IsType<TestHttpActivityFeature>(activityFeature);
        Assert.NotNull(activityFeature.Activity);
        Assert.Equal(HostingApplicationDiagnostics.ActivityName, activityFeature.Activity.DisplayName);
        var initialActivity = Activity.Current;

        // Create nested dummy Activity
        using var _ = dummySource.StartActivity("DummyActivity");

        Assert.Same(initialActivity, activityFeature.Activity);
        Assert.Null(activityFeature.Activity.ParentId);
        Assert.Equal(activityFeature.Activity.Id, Activity.Current.ParentId);
        Assert.NotEqual(Activity.Current, activityFeature.Activity);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
    }

    [Fact]
    public void IHttpActivityFeatureIsNotPopulatedWithoutAListener()
    {
        var hostingApplication = CreateApplication();
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpActivityFeature>(new TestHttpActivityFeature());
        var context = hostingApplication.CreateContext(httpContext.Features);

        var activityFeature = context.HttpContext.Features.Get<IHttpActivityFeature>();
        Assert.NotNull(activityFeature);
        Assert.Null(activityFeature.Activity);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
    }

    private static HostingApplication CreateApplication(IHttpContextFactory httpContextFactory = null, bool useHttpContextAccessor = false,
        ActivitySource activitySource = null, IMeterFactory meterFactory = null)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        if (useHttpContextAccessor)
        {
            services.AddHttpContextAccessor();
        }

        httpContextFactory ??= new DefaultHttpContextFactory(services.BuildServiceProvider());

        var hostingApplication = new HostingApplication(
            ctx => Task.CompletedTask,
            NullLogger.Instance,
            new DiagnosticListener("Microsoft.AspNetCore"),
            activitySource ?? new ActivitySource("Microsoft.AspNetCore"),
            DistributedContextPropagator.CreateDefaultPropagator(),
            httpContextFactory,
            HostingEventSource.Log,
            new HostingMetrics(meterFactory ?? new TestMeterFactory()));

        return hostingApplication;
    }

    private class FeaturesWithContext<T> : IHostContextContainer<T>, IFeatureCollection
    {
        public FeaturesWithContext(IFeatureCollection features)
        {
            Features = features;
        }

        public IFeatureCollection Features { get; }

        public object this[Type key] { get => Features[key]; set => Features[key] = value; }

        public T HostContext { get; set; }

        public bool IsReadOnly => Features.IsReadOnly;

        public int Revision => Features.Revision;

        public TFeature Get<TFeature>() => Features.Get<TFeature>();

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => Features.GetEnumerator();

        public void Set<TFeature>(TFeature instance) => Features.Set(instance);

        IEnumerator IEnumerable.GetEnumerator() => Features.GetEnumerator();
    }
}
