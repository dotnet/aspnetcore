// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingApplicationTests
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
        public void CreateContextSetsCorrelationIdInScope()
        {
            // Arrange
            var logger = new LoggerWithScopes();
            var hostingApplication = CreateApplication(out var features, logger: logger);
            features.Get<IHttpRequestFeature>().Headers["Request-Id"] = "some correlation id";

            // Act
            var context = hostingApplication.CreateContext(features);

            Assert.Single(logger.Scopes);
            var pairs = ((IReadOnlyList<KeyValuePair<string, object>>)logger.Scopes[0]).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("some correlation id", pairs["CorrelationId"].ToString());
        }

        [Fact]
        public void ActivityIsNotCreatedWhenIsEnabledForActivityIsFalse()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool eventsFired = false;
            bool isEnabledActivityFired = false;
            bool isEnabledStartFired = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool eventsFired = false;
            bool isEnabledStartFired = false;
            bool isEnabledActivityFired = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool startCalled = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair => { }),
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
            var diagnosticSource = new DiagnosticListener("DummySource");
            var hostingApplication = CreateApplication(out var features, diagnosticSource: diagnosticSource);

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair => { }),
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
            DiagnosticListener diagnosticSource = null, ILogger logger = null)
        {
            var httpContextFactory = new Mock<IHttpContextFactory>();

            features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            httpContextFactory.Setup(s => s.Create(It.IsAny<IFeatureCollection>())).Returns(new DefaultHttpContext(features));
            httpContextFactory.Setup(s => s.Dispose(It.IsAny<HttpContext>()));

            var hostingApplication = new HostingApplication(
                ctx => Task.FromResult(0),
                logger ?? new NullScopeLogger(),
                diagnosticSource ?? new NoopDiagnosticSource(),
                httpContextFactory.Object);

            return hostingApplication;
        }

        private class NullScopeLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }

        private class LoggerWithScopes : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                Scopes.Add(state);
                return new Scope();
            }

            public List<object> Scopes { get; set; } = new List<object>();

            public bool IsEnabled(LogLevel logLevel) => true;

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

        private class NoopDiagnosticSource : DiagnosticListener
        {
            public NoopDiagnosticSource() : base("DummyListener")
            {
            }

            public override bool IsEnabled(string name) => true;

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
