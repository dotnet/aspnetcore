// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingApplicationDiagnosticsTests
{
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
        DiagnosticListener diagnosticListener = null, ActivitySource activitySource = null, ILogger logger = null, Action<DefaultHttpContext> configure = null)
    {
        var httpContextFactory = new Mock<IHttpContextFactory>();

        features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
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
            httpContextFactory.Object);

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
