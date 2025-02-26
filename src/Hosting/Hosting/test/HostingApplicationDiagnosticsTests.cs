// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingApplicationDiagnosticsTests : LoggedTest
{
    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57259")]
    public async Task EventCountersAndMetricsValues()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        // requests-per-second isn't tested because the value can't be reliably tested because of time
        using var eventListener = new TestCounterListener(LoggerFactory, hostingEventSource.Name,
        [
            "total-requests",
            "current-requests",
            "failed-requests"
        ]);

        var timeout = !Debugger.IsAttached ? TimeSpan.FromSeconds(30) : Timeout.InfiniteTimeSpan;
        using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout);
        timeoutTokenSource.Token.Register(() => Logger.LogError("Timeout while waiting for counter value."));

        var totalRequestValues = eventListener.GetCounterValues("total-requests", timeoutTokenSource.Token);
        var currentRequestValues = eventListener.GetCounterValues("current-requests", timeoutTokenSource.Token);
        var failedRequestValues = eventListener.GetCounterValues("failed-requests", timeoutTokenSource.Token);

        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                { "EventCounterIntervalSec", "1" }
            });

        var testMeterFactory1 = new TestMeterFactory();
        var testMeterFactory2 = new TestMeterFactory();

        var logger = LoggerFactory.CreateLogger<HostingApplication>();
        var hostingApplication1 = CreateApplication(out var features1, eventSource: hostingEventSource, meterFactory: testMeterFactory1, logger: logger);
        var hostingApplication2 = CreateApplication(out var features2, eventSource: hostingEventSource, meterFactory: testMeterFactory2, logger: logger);

        using var activeRequestsCollector1 = new MetricCollector<long>(testMeterFactory1, HostingMetrics.MeterName, "http.server.active_requests");
        using var activeRequestsCollector2 = new MetricCollector<long>(testMeterFactory2, HostingMetrics.MeterName, "http.server.active_requests");
        using var requestDurationCollector1 = new MetricCollector<double>(testMeterFactory1, HostingMetrics.MeterName, "http.server.request.duration");
        using var requestDurationCollector2 = new MetricCollector<double>(testMeterFactory2, HostingMetrics.MeterName, "http.server.request.duration");

        // Act/Assert 1
        Logger.LogInformation("Act/Assert 1");
        Logger.LogInformation(nameof(HostingApplication.CreateContext));

        var context1 = hostingApplication1.CreateContext(features1);
        var context2 = hostingApplication2.CreateContext(features2);

        await WaitForCounterValue(totalRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        Logger.LogInformation(nameof(HostingApplication.DisposeContext));

        hostingApplication1.DisposeContext(context1, null);
        hostingApplication2.DisposeContext(context2, null);

        await WaitForCounterValue(totalRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 0, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        Assert.Collection(activeRequestsCollector1.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(activeRequestsCollector2.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector1.GetMeasurementSnapshot(),
            m => Assert.True(m.Value > 0));
        Assert.Collection(requestDurationCollector2.GetMeasurementSnapshot(),
            m => Assert.True(m.Value > 0));

        // Act/Assert 2
        Logger.LogInformation("Act/Assert 2");
        Logger.LogInformation(nameof(HostingApplication.CreateContext));

        context1 = hostingApplication1.CreateContext(features1);
        context2 = hostingApplication2.CreateContext(features2);

        await WaitForCounterValue(totalRequestValues, expectedValue: 4, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        context1.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context2.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        Logger.LogInformation(nameof(HostingApplication.DisposeContext));

        hostingApplication1.DisposeContext(context1, null);
        hostingApplication2.DisposeContext(context2, null);

        await WaitForCounterValue(totalRequestValues, expectedValue: 4, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 0, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 2, Logger);

        Assert.Collection(activeRequestsCollector1.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(activeRequestsCollector2.GetMeasurementSnapshot(),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value),
            m => Assert.Equal(1, m.Value),
            m => Assert.Equal(-1, m.Value));
        Assert.Collection(requestDurationCollector1.GetMeasurementSnapshot(),
            m => Assert.True(m.Value > 0),
            m => Assert.True(m.Value > 0));
        Assert.Collection(requestDurationCollector2.GetMeasurementSnapshot(),
            m => Assert.True(m.Value > 0),
            m => Assert.True(m.Value > 0));
    }

    private static async Task WaitForCounterValue(CounterValues values, double expectedValue, ILogger logger)
    {
        await values.Values.WaitForValueAsync(expectedValue, values.CounterName, logger);
    }

    [Fact]
    public void EventCountersEnabled()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        using var eventListener = new TestCounterListener(LoggerFactory, hostingEventSource.Name,
        [
            "requests-per-second",
            "total-requests",
            "current-requests",
            "failed-requests"
        ]);

        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                { "EventCounterIntervalSec", "1" }
            });

        var testMeterFactory = new TestMeterFactory();

        // Act
        var hostingApplication = CreateApplication(out var features, eventSource: hostingEventSource, meterFactory: testMeterFactory);
        var context = hostingApplication.CreateContext(features);

        // Assert
        Assert.True(context.EventLogEnabled);
        Assert.False(context.MetricsEnabled);
    }

    [Fact]
    public void Metrics_RequestDuration_RecordedWithHttpActivity()
    {
        // Arrange
        Activity measurementActivity = null;
        var measureCount = 0;

        // Listen to hosting activity source.
        var testSource = new ActivitySource(Path.GetRandomFileName());
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = activitySource => ReferenceEquals(activitySource, testSource),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        // Listen to http.server.request.duration.
        var testMeterFactory = new TestMeterFactory();
        var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Scope == testMeterFactory && i.Meter.Name == HostingMetrics.MeterName && i.Name == "http.server.request.duration")
            {
                l.EnableMeasurementEvents(i);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((i, m, t, s) =>
        {
            if (Interlocked.Increment(ref measureCount) > 1)
            {
                throw new Exception("Unexpected measurement count.");
            }

            measurementActivity = Activity.Current;
        });
        meterListener.Start();

        // Act
        var hostingApplication = CreateApplication(out var features, activitySource: testSource, meterFactory: testMeterFactory);
        var context = hostingApplication.CreateContext(features);
        hostingApplication.DisposeContext(context, null);

        // Assert
        Assert.Equal(1, measureCount);
        Assert.NotNull(measurementActivity);
        Assert.Equal(HostingApplicationDiagnostics.ActivityName, measurementActivity.OperationName);
    }

    [Fact]
    public void MetricsEnabled()
    {
        // Arrange
        var testMeterFactory = new TestMeterFactory();
        using var activeRequestsCollector = new MetricCollector<long>(testMeterFactory, HostingMetrics.MeterName, "http.server.active_requests");
        using var requestDurationCollector = new MetricCollector<double>(testMeterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        var hostingApplication = CreateApplication(out var features, meterFactory: testMeterFactory);
        var context = hostingApplication.CreateContext(features);

        // Assert
        Assert.True(context.MetricsEnabled);
        Assert.False(context.EventLogEnabled);
    }

    [Fact]
    public void Metrics_RequestChanges_OriginalValuesUsed()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        var testMeterFactory = new TestMeterFactory();
        using var activeRequestsCollector = new MetricCollector<long>(testMeterFactory, HostingMetrics.MeterName, "http.server.active_requests");

        // Act
        var hostingApplication = CreateApplication(out var features, eventSource: hostingEventSource, meterFactory: testMeterFactory, configure: c =>
        {
            c.Request.Protocol = "1.1";
            c.Request.Scheme = "http";
            c.Request.Method = "POST";
            c.Request.Host = new HostString("localhost");
            c.Request.Path = "/hello";
            c.Request.ContentType = "text/plain";
            c.Request.ContentLength = 1024;
        });
        var context = hostingApplication.CreateContext(features);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });

        context.HttpContext.Request.Protocol = "HTTP/2";
        context.HttpContext.Request.Method = "PUT";
        context.HttpContext.Request.Scheme = "https";
        context.HttpContext.Features.GetRequiredFeature<IHttpMetricsTagsFeature>().Tags.Add(new KeyValuePair<string, object>("custom.tag", "custom.value"));

        hostingApplication.DisposeContext(context, null);

        // Assert
        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            },
            m =>
            {
                Assert.Equal(-1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });

        Assert.Empty(context.MetricsTagsFeature.TagsList);
        Assert.Null(context.MetricsTagsFeature.Scheme);
        Assert.Null(context.MetricsTagsFeature.Method);
        Assert.Null(context.MetricsTagsFeature.Protocol);
    }

    [Fact]
    public void Metrics_Route_RouteTagReported()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        var testMeterFactory = new TestMeterFactory();
        using var activeRequestsCollector = new MetricCollector<long>(testMeterFactory, HostingMetrics.MeterName, "http.server.active_requests");
        using var requestDurationCollector = new MetricCollector<double>(testMeterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        var hostingApplication = CreateApplication(out var features, eventSource: hostingEventSource, meterFactory: testMeterFactory, configure: c =>
        {
            c.Request.Protocol = "1.1";
            c.Request.Scheme = "http";
            c.Request.Method = "POST";
            c.Request.Host = new HostString("localhost");
            c.Request.Path = "/hello";
            c.Request.ContentType = "text/plain";
            c.Request.ContentLength = 1024;
        });
        var context = hostingApplication.CreateContext(features);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });

        context.HttpContext.SetEndpoint(new Endpoint(
            c => Task.CompletedTask,
            new EndpointMetadataCollection(new TestRouteDiagnosticsMetadata()),
            "Test endpoint"));

        hostingApplication.DisposeContext(context, null);

        // Assert
        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            },
            m =>
            {
                Assert.Equal(-1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });
        Assert.Collection(requestDurationCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal("hello/{name}", m.Tags["http.route"]);
            });
    }

    [Fact]
    public void Metrics_DisableHttpMetricsWithMetadata_NoMetrics()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        var testMeterFactory = new TestMeterFactory();
        using var activeRequestsCollector = new MetricCollector<long>(testMeterFactory, HostingMetrics.MeterName, "http.server.active_requests");
        using var requestDurationCollector = new MetricCollector<double>(testMeterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        var hostingApplication = CreateApplication(out var features, eventSource: hostingEventSource, meterFactory: testMeterFactory, configure: c =>
        {
            c.Request.Protocol = "1.1";
            c.Request.Scheme = "http";
            c.Request.Method = "POST";
            c.Request.Host = new HostString("localhost");
            c.Request.Path = "/hello";
            c.Request.ContentType = "text/plain";
            c.Request.ContentLength = 1024;
        });
        var context = hostingApplication.CreateContext(features);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });

        context.HttpContext.SetEndpoint(new Endpoint(
            c => Task.CompletedTask,
            new EndpointMetadataCollection(new TestRouteDiagnosticsMetadata(), new DisableHttpMetricsAttribute()),
            "Test endpoint"));

        hostingApplication.DisposeContext(context, null);

        // Assert
        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            },
            m =>
            {
                Assert.Equal(-1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });
        Assert.Empty(requestDurationCollector.GetMeasurementSnapshot());
    }

    [Fact]
    public void Metrics_DisableHttpMetricsWithFeature_NoMetrics()
    {
        // Arrange
        var hostingEventSource = new HostingEventSource(Guid.NewGuid().ToString());

        var testMeterFactory = new TestMeterFactory();
        using var activeRequestsCollector = new MetricCollector<long>(testMeterFactory, HostingMetrics.MeterName, "http.server.active_requests");
        using var requestDurationCollector = new MetricCollector<double>(testMeterFactory, HostingMetrics.MeterName, "http.server.request.duration");

        // Act
        var hostingApplication = CreateApplication(out var features, eventSource: hostingEventSource, meterFactory: testMeterFactory, configure: c =>
        {
            c.Request.Protocol = "1.1";
            c.Request.Scheme = "http";
            c.Request.Method = "POST";
            c.Request.Host = new HostString("localhost");
            c.Request.Path = "/hello";
            c.Request.ContentType = "text/plain";
            c.Request.ContentLength = 1024;
        });
        var context = hostingApplication.CreateContext(features);

        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });

        context.HttpContext.Features.Get<IHttpMetricsTagsFeature>().MetricsDisabled = true;

        // Assert 1
        Assert.True(context.MetricsTagsFeature.MetricsDisabled);

        hostingApplication.DisposeContext(context, null);

        // Assert 2
        Assert.Collection(activeRequestsCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.Equal(1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            },
            m =>
            {
                Assert.Equal(-1, m.Value);
                Assert.Equal("http", m.Tags["url.scheme"]);
                Assert.Equal("POST", m.Tags["http.request.method"]);
            });
        Assert.Empty(requestDurationCollector.GetMeasurementSnapshot());
        Assert.False(context.MetricsTagsFeature.MetricsDisabled);
    }

    private sealed class TestRouteDiagnosticsMetadata : IRouteDiagnosticsMetadata
    {
        public string Route { get; } = "hello/{name}";
    }

    [Fact]
    public void DisposeContextDoesNotThrowWhenContextScopeIsNull()
    {
        // Arrange
        var hostingApplication = CreateApplication(out var features);
        var context = hostingApplication.CreateContext(features);

        // Act/Assert
        hostingApplication.DisposeContext(context, null);
    }

    [Fact]
    public void CreateContextWithDisabledLoggerDoesNotCreateActivity()
    {
        // Arrange
        var hostingApplication = CreateApplication(out var features);

        // Act
        hostingApplication.CreateContext(features);

        Assert.Null(Activity.Current);
    }

    [Fact]
    public void ActivityStopDoesNotFireIfNoListenerAttachedForStart()
    {
        // Arrange
        var diagnosticListener = new DiagnosticListener("DummySource");
        var logger = new LoggerWithScopes(isEnabled: true);
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener, logger: logger);
        var startFired = false;
        var stopFired = false;

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            // This should not fire
            if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
            {
                startFired = true;
            }

            // This should not fire
            if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
            {
                stopFired = true;
            }
        }),
        (s, o, arg3) =>
        {
            // The events are off
            return false;
        });

        // Act
        var context = hostingApplication.CreateContext(features);

        hostingApplication.DisposeContext(context, exception: null);

        Assert.False(startFired);
        Assert.False(stopFired);
        Assert.Null(Activity.Current);
    }

    [Fact]
    public void ActivityIsNotCreatedWhenIsEnabledForActivityIsFalse()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool eventsFired = false;
        bool isEnabledActivityFired = false;
        bool isEnabledStartFired = false;

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal);
        }), (s, o, arg3) =>
        {
            if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
            {
                Assert.IsAssignableFrom<HttpContext>(o);
                isEnabledActivityFired = true;
            }
            if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
            {
                isEnabledStartFired = true;
            }
            return false;
        });

        hostingApplication.CreateContext(features);
        Assert.Null(Activity.Current);
        Assert.True(isEnabledActivityFired);
        Assert.False(isEnabledStartFired);
        Assert.False(eventsFired);
    }

    [Fact]
    public void ActivityIsCreatedButNotLoggedWhenIsEnabledForActivityStartIsFalse()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool eventsFired = false;
        bool isEnabledStartFired = false;
        bool isEnabledActivityFired = false;

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal);
        }), (s, o, arg3) =>
        {
            if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
            {
                Assert.IsAssignableFrom<HttpContext>(o);
                isEnabledActivityFired = true;
                return true;
            }

            if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
            {
                isEnabledStartFired = true;
                return false;
            }
            return true;
        });

        hostingApplication.CreateContext(features);
        Assert.NotNull(Activity.Current);
        Assert.True(isEnabledActivityFired);
        Assert.True(isEnabledStartFired);
        Assert.False(eventsFired);
    }

    [Fact]
    public void ActivityIsCreatedAndLogged()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool startCalled = false;

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
            {
                startCalled = true;
                Assert.NotNull(pair.Value);
                Assert.NotNull(Activity.Current);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                AssertProperty<HttpContext>(pair.Value, "HttpContext");
            }
        }));

        hostingApplication.CreateContext(features);
        Assert.NotNull(Activity.Current);
        Assert.True(startCalled);
    }

    [Fact]
    public void ActivityIsStoppedDuringStopCall()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool endCalled = false;
        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
            {
                endCalled = true;

                Assert.NotNull(Activity.Current);
                Assert.True(Activity.Current.Duration > TimeSpan.Zero);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                AssertProperty<HttpContext>(pair.Value, "HttpContext");
            }
        }));

        var context = hostingApplication.CreateContext(features);
        hostingApplication.DisposeContext(context, null);
        Assert.True(endCalled);
    }

    [Fact]
    public void ActivityIsStoppedDuringUnhandledExceptionCall()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool endCalled = false;
        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
            {
                endCalled = true;
                Assert.NotNull(Activity.Current);
                Assert.True(Activity.Current.Duration > TimeSpan.Zero);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                AssertProperty<HttpContext>(pair.Value, "HttpContext");
            }
        }));

        var context = hostingApplication.CreateContext(features);
        hostingApplication.DisposeContext(context, new Exception());
        Assert.True(endCalled);
    }

    [Fact]
    public void ActivityIsAvailableDuringUnhandledExceptionCall()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool endCalled = false;
        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
        {
            if (pair.Key == "Microsoft.AspNetCore.Hosting.UnhandledException")
            {
                endCalled = true;
                Assert.NotNull(Activity.Current);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
            }
        }));

        var context = hostingApplication.CreateContext(features);
        hostingApplication.DisposeContext(context, new Exception());
        Assert.True(endCalled);
    }

    [Fact]
    public void ActivityIsAvailibleDuringRequest()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        hostingApplication.CreateContext(features);

        Assert.NotNull(Activity.Current);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
    }

    [Fact]
    public void ActivityParentIdAndBaggageReadFromHeaders()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"Request-Id", "ParentId1"},
                {"baggage", "Key1=value1, Key2=value2"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Equal("ParentId1", Activity.Current.ParentId);
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");
    }

    [Fact]
    public void BaggageReadFromHeadersWithoutRequestId()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"baggage", "Key1=value1, Key2=value2"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");
    }

    [Fact]
    public void ActivityBaggageReadFromLegacyHeaders()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"Request-Id", "ParentId1"},
                {"Correlation-Context", "Key1=value1, Key2=value2"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");
    }

    [Fact]
    public void ActivityBaggagePrefersW3CBaggageHeaderName()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"Request-Id", "ParentId1"},
                {"Correlation-Context", "Key1=value1, Key2=value2"},
                {"baggage", "Key1=value3, Key2=value4"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value3");
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value4");
    }

    [Fact]
    public void ActivityBaggagePreservesItemsOrder()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"Request-Id", "ParentId1"},
                {"baggage", "Key1=value1, Key2=value2, Key1=value3"} // duplicated keys allowed by the contract
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);

        var expectedBaggage = new[]
        {
            KeyValuePair.Create("Key1","value1"),
            KeyValuePair.Create("Key2","value2"),
            KeyValuePair.Create("Key1","value3")
        };

        Assert.Equal(expectedBaggage, Activity.Current.Baggage.ToArray());
    }

    [Fact]
    public void ActivityBaggageValuesAreUrlDecodedFromHeaders()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"Request-Id", "ParentId1"},
                {"baggage", "Key1=value1%2F1"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1/1");
    }

    [Fact]
    public void ActivityTraceParentAndTraceStateFromHeaders()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
            s =>
            {
                if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"traceparent", "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01"},
                {"tracestate", "TraceState1"},
                {"baggage", "Key1=value1, Key2=value2"}
            }
        });
        hostingApplication.CreateContext(features);
        Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
        Assert.Equal(ActivityIdFormat.W3C, Activity.Current.IdFormat);
        Assert.Equal("0123456789abcdef0123456789abcdef", Activity.Current.TraceId.ToHexString());
        Assert.Equal("0123456789abcdef", Activity.Current.ParentSpanId.ToHexString());
        Assert.Equal("TraceState1", Activity.Current.TraceStateString);

        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
        Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");
    }

    [Fact]
    public void SamplersReceiveCorrectParentAndTraceIds()
    {
        var testSource = new ActivitySource(Path.GetRandomFileName());
        var hostingApplication = CreateApplication(out var features, activitySource: testSource);
        var parentId = "";
        var parentSpanId = "";
        var traceId = "";
        using var listener = new ActivityListener
        {
            ShouldListenTo = activitySource => ReferenceEquals(activitySource, testSource),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ComputeActivitySamplingResult(ref options),
            ActivityStarted = activity =>
            {
                parentId = activity.ParentId;
                parentSpanId = activity.ParentSpanId.ToHexString();
                traceId = activity.TraceId.ToHexString();
            }
        };

        ActivitySource.AddActivityListener(listener);

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"traceparent", "00-35aae61e3e99044eb5ea5007f2cd159b-40a8bd87c078cb4c-00"},
            }
        });

        hostingApplication.CreateContext(features);
        Assert.Equal("00-35aae61e3e99044eb5ea5007f2cd159b-40a8bd87c078cb4c-00", parentId);
        Assert.Equal("40a8bd87c078cb4c", parentSpanId);
        Assert.Equal("35aae61e3e99044eb5ea5007f2cd159b", traceId);

        static ActivitySamplingResult ComputeActivitySamplingResult(ref ActivityCreationOptions<ActivityContext> options)
        {
            Assert.Equal("35aae61e3e99044eb5ea5007f2cd159b", options.TraceId.ToHexString());
            Assert.Equal("40a8bd87c078cb4c", options.Parent.SpanId.ToHexString());

            return ActivitySamplingResult.AllDataAndRecorded;
        }
    }

    [Fact]
    public void ActivityOnImportHookIsCalled()
    {
        var diagnosticListener = new DiagnosticListener("DummySource");
        var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

        bool onActivityImportCalled = false;
        diagnosticListener.Subscribe(
            observer: new CallbackDiagnosticListener(pair => { }),
            isEnabled: (s, o, _) => true,
            onActivityImport: (activity, context) =>
            {
                onActivityImportCalled = true;
                Assert.Null(Activity.Current);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", activity.OperationName);
                Assert.NotNull(context);
                Assert.IsAssignableFrom<HttpContext>(context);

                activity.ActivityTraceFlags = ActivityTraceFlags.Recorded;
            });

        hostingApplication.CreateContext(features);

        Assert.True(onActivityImportCalled);
        Assert.NotNull(Activity.Current);
        Assert.True(Activity.Current.Recorded);
    }

    [Fact]
    public void ActivityListenersAreCalled()
    {
        var testSource = new ActivitySource(Path.GetRandomFileName());
        var hostingApplication = CreateApplication(out var features, activitySource: testSource);
        var parentSpanId = "";
        using var listener = new ActivityListener
        {
            ShouldListenTo = activitySource => ReferenceEquals(activitySource, testSource),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                parentSpanId = Activity.Current.ParentSpanId.ToHexString();
            }
        };

        ActivitySource.AddActivityListener(listener);

        features.Set<IHttpRequestFeature>(new HttpRequestFeature()
        {
            Headers = new HeaderDictionary()
            {
                {"traceparent", "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01"},
                {"tracestate", "TraceState1"},
                {"baggage", "Key1=value1, Key2=value2"}
            }
        });

        hostingApplication.CreateContext(features);
        Assert.Equal("0123456789abcdef", parentSpanId);
    }

    [Fact]
    public void RequestLogs()
    {
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);

        var hostingApplication = CreateApplication(out var features, logger: loggerFactory.CreateLogger("Test"), configure: c =>
        {
            c.Request.Protocol = "1.1";
            c.Request.Scheme = "http";
            c.Request.Method = "POST";
            c.Request.Host = new HostString("localhost");
            c.Request.Path = "/hello";
            c.Request.ContentType = "text/plain";
            c.Request.ContentLength = 1024;
        });

        var context = hostingApplication.CreateContext(features);

        context.HttpContext.Items["__RequestUnhandled"] = true;
        context.HttpContext.Response.StatusCode = 404;

        hostingApplication.DisposeContext(context, exception: null);

        var startLog = testSink.Writes.Single(w => w.EventId == LoggerEventIds.RequestStarting);
        var unhandedLog = testSink.Writes.Single(w => w.EventId == LoggerEventIds.RequestUnhandled);
        var endLog = testSink.Writes.Single(w => w.EventId == LoggerEventIds.RequestFinished);

        Assert.Equal("Request starting 1.1 POST http://localhost/hello - text/plain 1024", startLog.Message);
        Assert.Equal("Request reached the end of the middleware pipeline without being handled by application code. Request path: POST http://localhost/hello, Response status code: 404", unhandedLog.Message);
        Assert.StartsWith("Request finished 1.1 POST http://localhost/hello - 404", endLog.Message);
    }

    private static void AssertProperty<T>(object o, string name)
    {
        Assert.NotNull(o);
        var property = o.GetType().GetTypeInfo().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        var value = property.GetValue(o);
        Assert.NotNull(value);
        Assert.IsAssignableFrom<T>(value);
    }

    private static HostingApplication CreateApplication(out FeatureCollection features,
        DiagnosticListener diagnosticListener = null, ActivitySource activitySource = null, ILogger logger = null,
        Action<DefaultHttpContext> configure = null, HostingEventSource eventSource = null, IMeterFactory meterFactory = null)
    {
        var httpContextFactory = new Mock<IHttpContextFactory>();

        features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        var context = new DefaultHttpContext(features);
        configure?.Invoke(context);
        httpContextFactory.Setup(s => s.Create(It.IsAny<IFeatureCollection>())).Returns(context);
        httpContextFactory.Setup(s => s.Dispose(It.IsAny<HttpContext>()));

        var hostingApplication = new HostingApplication(
            ctx => Task.CompletedTask,
            logger ?? new NullScopeLogger(),
            diagnosticListener ?? new NoopDiagnosticListener(),
            activitySource ?? new ActivitySource("Microsoft.AspNetCore"),
            DistributedContextPropagator.CreateDefaultPropagator(),
            httpContextFactory.Object,
            eventSource ?? HostingEventSource.Log,
            new HostingMetrics(meterFactory ?? new TestMeterFactory()));

        return hostingApplication;
    }

    private class NullScopeLogger : ILogger
    {
        private readonly bool _isEnabled;
        public NullScopeLogger(bool isEnabled = false)
        {
            _isEnabled = isEnabled;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => _isEnabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }

    private class LoggerWithScopes : ILogger
    {
        private readonly bool _isEnabled;
        public LoggerWithScopes(bool isEnabled = false)
        {
            _isEnabled = isEnabled;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            Scopes.Add(state);
            return new Scope();
        }

        public List<object> Scopes { get; set; } = new List<object>();

        public bool IsEnabled(LogLevel logLevel) => _isEnabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {

        }

        private class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private class NoopDiagnosticListener : DiagnosticListener
    {
        private readonly bool _isEnabled;

        public NoopDiagnosticListener(bool isEnabled = false) : base("DummyListener")
        {
            _isEnabled = isEnabled;
        }

        public override bool IsEnabled(string name) => _isEnabled;

        public override void Write(string name, object value)
        {
        }
    }

    private class CallbackDiagnosticListener : IObserver<KeyValuePair<string, object>>
    {
        private readonly Action<KeyValuePair<string, object>> _callback;

        public CallbackDiagnosticListener(Action<KeyValuePair<string, object>> callback)
        {
            _callback = callback;
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            _callback(value);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
