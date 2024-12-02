// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Microsoft.AspNetCore.Hosting.HostingApplication;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingApplicationTests
{
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
        using var dummyActivity = dummySource.StartActivity("DummyActivity");
        Assert.NotNull(dummyActivity);
        Assert.Equal(Activity.Current, dummyActivity);

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
    public void IHttpActivityFeatureNotUsedFromFeatureCollection()
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

        // This feature will be overidden by hosting. Hosting is the owner of the feature and is resposible for setting it.
        var overridenFeature = new TestHttpActivityFeature();
        httpContext.Features.Set<IHttpActivityFeature>(overridenFeature);

        var context = hostingApplication.CreateContext(httpContext.Features);

        var contextFeature = context.HttpContext.Features.Get<IHttpActivityFeature>();
        Assert.NotNull(contextFeature);
        Assert.NotNull(contextFeature.Activity);
        Assert.Equal(HostingApplicationDiagnostics.ActivityName, contextFeature.Activity.DisplayName);

        Assert.NotEqual(overridenFeature, contextFeature);
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
