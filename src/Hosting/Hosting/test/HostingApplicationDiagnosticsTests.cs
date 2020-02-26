// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
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
        public void CreateContextWithEnabledLoggerCreatesActivityAndSetsActivityInScope()
        {
            // Arrange
            var logger = new LoggerWithScopes(isEnabled: true);
            var hostingApplication = CreateApplication(out var features, logger: logger);

            // Act
            var context = hostingApplication.CreateContext(features);

            Assert.Single(logger.Scopes);
            var pairs = ((IReadOnlyList<KeyValuePair<string, object>>)logger.Scopes[0]).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(Activity.Current.Id, pairs["SpanId"].ToString());
            Assert.Equal(Activity.Current.RootId, pairs["TraceId"].ToString());
            Assert.Equal(string.Empty, pairs["ParentId"]?.ToString());
        }

        [Fact]
        public void CreateContextWithEnabledLoggerAndRequestIdCreatesActivityAndSetsActivityInScope()
        {
            // Arrange

            // Generate an id we can use for the request id header (in the correct format)
            var activity = new Activity("IncomingRequest");
            activity.Start();
            var id = activity.Id;
            activity.Stop();

            var logger = new LoggerWithScopes(isEnabled: true);
            var hostingApplication = CreateApplication(out var features, logger: logger, configure: context =>
            {
                context.Request.Headers["Request-Id"] = id;
            });

            // Act
            var context = hostingApplication.CreateContext(features);

            Assert.Single(logger.Scopes);
            var pairs = ((IReadOnlyList<KeyValuePair<string, object>>)logger.Scopes[0]).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(Activity.Current.Id, pairs["SpanId"].ToString());
            Assert.Equal(Activity.Current.RootId, pairs["TraceId"].ToString());
            Assert.Equal(id, pairs["ParentId"].ToString());
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
                eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn");
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
                eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn");
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
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
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
        public void ActivityParentIdAndBaggeReadFromHeaders()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
                s =>
                {
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
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
            Assert.Equal("ParentId1", Activity.Current.ParentId);
            Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
            Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");
        }

        [Fact]
        public void ActivityBaggageValuesAreUrlDecodedFromHeaders()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticListener: diagnosticListener);

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair => { }),
                s =>
                {
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
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
                    {"Correlation-Context", "Key1=value1%2F1"}
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
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
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
                    {"Correlation-Context", "Key1=value1, Key2=value2"}
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
        public void ActivityOnExportHookIsCalled()
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
            DiagnosticListener diagnosticListener = null, ILogger logger = null, Action<DefaultHttpContext> configure = null)
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
}
