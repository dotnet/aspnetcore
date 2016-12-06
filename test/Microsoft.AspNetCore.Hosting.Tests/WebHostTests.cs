// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public class WebHostTests : IServer
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();
        private IFeatureCollection _featuresSupportedByThisHost = NewFeatureCollection();

        public IFeatureCollection Features
        {
            get
            {
                var features = new FeatureCollection();

                foreach (var feature in _featuresSupportedByThisHost)
                {
                    features[feature.Key] = feature.Value;
                }

                return features;
            }
        }

        static IFeatureCollection NewFeatureCollection()
        {
            var stub = new StubFeatures();
            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(stub);
            features.Set<IHttpResponseFeature>(stub);
            features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            return features;
        }

        [Fact]
        public void WebHostThrowsWithNoServer()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => CreateBuilder().Build().Start());
            Assert.Equal("No service for type 'Microsoft.AspNetCore.Hosting.Server.IServer' has been registered.", ex.Message);
        }

        [Fact]
        public void UseStartupThrowsWithNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateBuilder().UseStartup((string)null));
        }

        [Fact]
        public void CanDefaultAddressesIfNotConfigured()
        {
            var host = CreateBuilder().UseServer(this).Build();
            host.Start();
            Assert.NotNull(host.Services.GetService<IHostingEnvironment>());
            Assert.Equal("http://localhost:5000", host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void UsesLegacyConfigurationForAddresses()
        {
            var data = new Dictionary<string, string>
            {
                { "server.urls", "http://localhost:5002" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var host = CreateBuilder(config).UseServer(this).Build();
            host.Start();
            Assert.Equal("http://localhost:5002", host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void UsesConfigurationForAddresses()
        {
            var data = new Dictionary<string, string>
            {
                { "urls", "http://localhost:5003" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var host = CreateBuilder(config).UseServer(this).Build();
            host.Start();
            Assert.Equal("http://localhost:5003", host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void UsesNewConfigurationOverLegacyConfigForAddresses()
        {
            var data = new Dictionary<string, string>
            {
                { "server.urls", "http://localhost:5003" },
                { "urls", "http://localhost:5009" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var host = CreateBuilder(config).UseServer(this).Build();
            host.Start();
            Assert.Equal("http://localhost:5009", host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void WebHostCanBeStarted()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Start();

            Assert.NotNull(host);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            host.Dispose();

            Assert.Equal(0, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void WebHostShutsDownWhenTokenTriggers()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();

            var cts = new CancellationTokenSource();

            Task.Run(() => host.Run(cts.Token));

            // Wait on the host to be started
            lifetime.ApplicationStarted.WaitHandle.WaitOne();

            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            cts.Cancel();

            // Wait on the host to shutdown
            lifetime.ApplicationStopped.WaitHandle.WaitOne();

            Assert.Equal(0, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void WebHostApplicationLifetimeEventsOrderedCorrectlyDuringShutdown()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            var applicationStartedEvent = new ManualResetEventSlim(false);
            var applicationStoppingEvent = new ManualResetEventSlim(false);
            var applicationStoppedEvent = new ManualResetEventSlim(false);
            var applicationStartedCompletedBeforeApplicationStopping = false;
            var applicationStoppingCompletedBeforeApplicationStopped = false;
            var applicationStoppedCompletedBeforeRunCompleted = false;

            lifetime.ApplicationStarted.Register(() =>
            {
                applicationStartedEvent.Set();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                // Check whether the applicationStartedEvent has been set
                applicationStartedCompletedBeforeApplicationStopping = applicationStartedEvent.IsSet;

                // Simulate work.
                Thread.Sleep(1000);

                applicationStoppingEvent.Set();
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                // Check whether the applicationStoppingEvent has been set
                applicationStoppingCompletedBeforeApplicationStopped = applicationStoppingEvent.IsSet;
                applicationStoppedEvent.Set();
            });

            var runHostAndVerifyApplicationStopped = Task.Run(() =>
            {
                host.Run();
                // Check whether the applicationStoppingEvent has been set
                applicationStoppedCompletedBeforeRunCompleted = applicationStoppedEvent.IsSet;
            });

            // Wait until application has started to shut down the host
            Assert.True(applicationStartedEvent.Wait(5000));

            // Trigger host shutdown on a separate thread
            Task.Run(() => lifetime.StopApplication());

            // Wait for all events and host.Run() to complete
            Assert.True(runHostAndVerifyApplicationStopped.Wait(5000));

            // Verify Ordering
            Assert.True(applicationStartedCompletedBeforeApplicationStopping);
            Assert.True(applicationStoppingCompletedBeforeApplicationStopped);
            Assert.True(applicationStoppedCompletedBeforeRunCompleted);
        }

        [Fact]
        public void WebHostDisposesServiceProvider()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(s =>
                {
                    s.AddTransient<IFakeService, FakeService>();
                    s.AddSingleton<IFakeSingletonService, FakeService>();
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build();

            host.Start();

            var singleton = (FakeService)host.Services.GetService<IFakeSingletonService>();
            var transient = (FakeService)host.Services.GetService<IFakeService>();

            Assert.False(singleton.Disposed);
            Assert.False(transient.Disposed);

            host.Dispose();

            Assert.True(singleton.Disposed);
            Assert.True(transient.Disposed);
        }

        [Fact]
        public void WebHostNotifiesApplicationStarted()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .Build();
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            using (host)
            {
                host.Start();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public void WebHostNotifiesAllIApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .Build();
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
            var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);
            var stopped = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopped);

            using (host)
            {
                host.Start();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.True(started.All(s => s));
                host.Dispose();
                Assert.True(stopping.All(s => s));
                Assert.True(stopped.All(s => s));
            }
        }

        [Fact]
        public void WebHostNotifiesAllIApplicationLifetimeEventsCallbacksEvenIfTheyThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(services =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build();

            using (host)
            {
                host.Start();
                Assert.True(events1[0]);
                Assert.True(events2[0]);
                host.Dispose();
                Assert.True(events1[1]);
                Assert.True(events2[1]);
            }
        }

        [Fact]
        public void WebHostStopApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;

            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(services =>
                {
                    Action started = () =>
                    {
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    services.AddSingleton<IHostedService>(new DelegateHostedService(started, stopping));
                })
                .Build();
            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            lifetime.StopApplication();

            using (host)
            {
                host.Start();

                Assert.Equal(0, stoppingCalls);
            }
        }

        [Fact]
        public void HostedServiceCanInjectApplicationLifetime()
        {
            var host = CreateBuilder()
                   .UseServer(this)
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<IHostedService, TestHostedService>();
                   })
                   .Build();
            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            lifetime.StopApplication();


            host.Start();
            var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
            Assert.True(svc.StartCalled);
            host.Dispose();
            Assert.True(svc.StopCalled);
        }

        [Fact]
        public void HostedServiceStartNotCalledIfWebHostNotStarted()
        {
            var host = CreateBuilder()
                   .UseServer(this)
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<IHostedService, TestHostedService>();
                   })
                   .Build();
            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            lifetime.StopApplication();

            var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
            Assert.False(svc.StartCalled);
            host.Dispose();
            Assert.False(svc.StopCalled);
        }

        [Fact]
        public void WebHostDisposeApplicationFiresStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;

            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(services =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    services.AddSingleton<IHostedService>(new DelegateHostedService(started, stopping));
                })
                .Build();
            var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            using (host)
            {
                host.Start();
                host.Dispose();

                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
            }
        }

        [Fact]
        public void WebHostNotifiesAllIHostedServicesAndIApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(services =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build();
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
            var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);

            using (host)
            {
                host.Start();
                Assert.True(events1[0]);
                Assert.True(events2[0]);
                Assert.True(started.All(s => s));
                host.Dispose();
                Assert.True(events1[1]);
                Assert.True(events2[1]);
                Assert.True(stopping.All(s => s));
            }
        }

        [Fact]
        public void WebHostInjectsHostingEnvironment()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .UseEnvironment("WithHostingEnvironment")
                .Build();

            using (host)
            {
                host.Start();
                var env = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal("Changed", env.EnvironmentName);
            }
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var builder = CreateBuilder()
                .ConfigureServices(services =>
                {
                    services.AddTransient<IStartup, TestStartup>();
                })
                .UseServer(this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            var host = CreateBuilder().UseServer(this).ConfigureServices(services => services.AddOptions()).Build();
            Assert.NotNull(host.Services.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void ConfiguresStartupFiltersInCorrectOrder()
        {
            // Verify ordering
            var configureOrder = 0;
            var host = CreateBuilder()
                .UseServer(this)
                .ConfigureServices(services =>
                {
                    services.AddTransient<IStartupFilter>(serviceProvider => new TestFilter(
                        () => Assert.Equal(1, configureOrder++),
                        () => Assert.Equal(2, configureOrder++),
                        () => Assert.Equal(5, configureOrder++)));
                    services.AddTransient<IStartupFilter>(serviceProvider => new TestFilter(
                        () => Assert.Equal(0, configureOrder++),
                        () => Assert.Equal(3, configureOrder++),
                        () => Assert.Equal(4, configureOrder++)));
                })
                .Build();
            Assert.Equal(6, configureOrder);
        }

        private class TestFilter : IStartupFilter
        {
            private readonly Action _verifyConfigureOrder;
            private readonly Action _verifyBuildBeforeOrder;
            private readonly Action _verifyBuildAfterOrder;

            public TestFilter(Action verifyConfigureOrder, Action verifyBuildBeforeOrder, Action verifyBuildAfterOrder)
            {
                _verifyConfigureOrder = verifyConfigureOrder;
                _verifyBuildBeforeOrder = verifyBuildBeforeOrder;
                _verifyBuildAfterOrder = verifyBuildAfterOrder;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                _verifyConfigureOrder();
                return builder =>
                {
                    _verifyBuildBeforeOrder();
                    next(builder);
                    _verifyBuildAfterOrder();
                };
            }
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            var host = CreateBuilder().UseServer(this).Build();
            var env = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal(EnvironmentName.Production, env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "Environment", "Staging" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var host = CreateBuilder(config).UseServer(this).Build();
            var env = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact(Skip = "Missing content publish property")]
        public void WebRootCanBeResolvedFromTheConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "webroot", "testroot" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var host = CreateBuilder(config).UseServer(this).Build();
            var env = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var host = CreateBuilder().UseServer(this).Build();
            using (host)
            {
                host.Start();
                var env = host.Services.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment(EnvironmentName.Production));
                Assert.True(env.IsEnvironment("producTion"));
            }
        }

        [Fact]
        public void WebHost_CreatesDefaultRequestIdentifierFeature_IfNotPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
                {
                    httpContext = innerHttpContext;
                    return Task.FromResult(0);
                });
            var host = CreateHost(requestDelegate);

            // Act
            host.Start();

            // Assert
            Assert.NotNull(httpContext);
            var featuresTraceIdentifier = httpContext.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
            Assert.False(string.IsNullOrWhiteSpace(httpContext.TraceIdentifier));
            Assert.Same(httpContext.TraceIdentifier, featuresTraceIdentifier);
        }

        [Fact]
        public void WebHost_DoesNot_CreateDefaultRequestIdentifierFeature_IfPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
            {
                httpContext = innerHttpContext;
                return Task.FromResult(0);
            });
            var requestIdentifierFeature = new StubHttpRequestIdentifierFeature();
            _featuresSupportedByThisHost[typeof(IHttpRequestIdentifierFeature)] = requestIdentifierFeature;
            var host = CreateHost(requestDelegate);

            // Act
            host.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.Same(requestIdentifierFeature, httpContext.Features.Get<IHttpRequestIdentifierFeature>());
        }

        [Fact]
        public void WebHost_InvokesConfigureMethodsOnlyOnce()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup<CountStartup>()
                .Build();
            using (host)
            {
                host.Start();
                var services = host.Services;
                var services2 = host.Services;
                Assert.Equal(1, CountStartup.ConfigureCount);
                Assert.Equal(1, CountStartup.ConfigureServicesCount);
            }
        }

        public class CountStartup
        {
            public static int ConfigureServicesCount;
            public static int ConfigureCount;

            public void ConfigureServices(IServiceCollection services)
            {
                ConfigureServicesCount++;
            }

            public void Configure(IApplicationBuilder app)
            {
                ConfigureCount++;
            }
        }

        [Fact]
        public void WebHost_ThrowsForBadConfigureServiceSignature()
        {
            var builder = CreateBuilder()
                .UseServer(this)
                .UseStartup<BadConfigureServicesStartup>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.True(ex.Message.Contains("ConfigureServices"));
        }

        public class BadConfigureServicesStartup
        {
            public void ConfigureServices(IServiceCollection services, int gunk) { }
            public void Configure(IApplicationBuilder app) { }
        }

        private IWebHost CreateHost(RequestDelegate requestDelegate)
        {
            var builder = CreateBuilder()
                .UseServer(this)
                .Configure(
                    appBuilder =>
                    {
                        appBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>().AddProvider(new AllMessagesAreNeeded());
                        appBuilder.Run(requestDelegate);
                    });
            return builder.Build();
        }

        private IWebHostBuilder CreateBuilder(IConfiguration config = null)
        {
            return new WebHostBuilder().UseConfiguration(config ?? new ConfigurationBuilder().Build()).UseStartup("Microsoft.AspNetCore.Hosting.Tests");
        }

        private static bool[] RegisterCallbacksThatThrow(IServiceCollection services)
        {
            bool[] events = new bool[2];

            Action started = () =>
            {
                events[0] = true;
                throw new InvalidOperationException();
            };

            Action stopping = () =>
            {
                events[1] = true;
                throw new InvalidOperationException();
            };

            services.AddSingleton<IHostedService>(new DelegateHostedService(started, stopping));

            return events;
        }

        private static bool[] RegisterCallbacksThatThrow(CancellationToken token)
        {
            var signals = new bool[3];
            for (int i = 0; i < signals.Length; i++)
            {
                token.Register(state =>
                {
                    signals[(int)state] = true;
                    throw new InvalidOperationException();
                }, i);
            }

            return signals;
        }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            var startInstance = new StartInstance();
            _startInstances.Add(startInstance);
            var context = application.CreateContext(Features);
            try
            {
                application.ProcessRequestAsync(context);
            }
            catch (Exception ex)
            {
                application.DisposeContext(context, ex);
                throw;
            }
            application.DisposeContext(context, null);
        }

        public void Dispose()
        {
            if (_startInstances != null)
            {
                foreach (var startInstance in _startInstances)
                {
                    startInstance.Dispose();
                }
            }
        }

        private class TestHostedService : IHostedService
        {
            private readonly IApplicationLifetime _lifetime;

            public TestHostedService(IApplicationLifetime lifetime)
            {
                _lifetime = lifetime;
            }

            public bool StartCalled { get; set; }
            public bool StopCalled { get; set; }

            public void Start()
            {
                StartCalled = true;
            }

            public void Stop()
            {
                StopCalled = true;
            }
        }

        private class DelegateHostedService : IHostedService
        {
            private readonly Action _started;
            private readonly Action _stopping;

            public DelegateHostedService(Action started, Action stopping)
            {
                _started = started;
                _stopping = stopping;
            }

            public void Start() => _started();

            public void Stop() => _stopping();
        }

        private class StartInstance : IDisposable
        {
            public int DisposeCalls { get; set; }

            public void Dispose()
            {
                DisposeCalls += 1;
            }
        }

        private class TestStartup : IStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                throw new NotImplementedException();
            }

            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                throw new NotImplementedException();
            }
        }

        private class ReadOnlyFeatureCollection : IFeatureCollection
        {
            public object this[Type key]
            {
                get { return null; }
                set { throw new NotSupportedException(); }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public int Revision
            {
                get { return 0; }
            }

            public void Dispose()
            {
            }

            public TFeature Get<TFeature>()
            {
                return default(TFeature);
            }

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
            {
                yield break;
            }

            public void Set<TFeature>(TFeature instance)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class AllMessagesAreNeeded : ILoggerProvider, ILogger
        {
            public bool IsEnabled(LogLevel logLevel) => true;

            public ILogger CreateLogger(string name) => this;

            public IDisposable BeginScope<TState>(TState state)
            {
                var stringified = state.ToString();
                return this;
            }
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var stringified = formatter(state, exception);
            }

            public void Dispose()
            {
            }
        }

        private class StubFeatures : IHttpRequestFeature, IHttpResponseFeature, IHeaderDictionary
        {
            public StubFeatures()
            {
                Headers = this;
                Body = new MemoryStream();
            }

            public StringValues this[string key]
            {
                get { return StringValues.Empty; }
                set { }
            }

            public Stream Body { get; set; }

            public int Count => 0;

            public bool HasStarted { get; set; }

            public IHeaderDictionary Headers { get; set; }

            public bool IsReadOnly => false;

            public ICollection<string> Keys => null;

            public string Method { get; set; }

            public string Path { get; set; }

            public string PathBase { get; set; }

            public string Protocol { get; set; }

            public string QueryString { get; set; }

            public string RawTarget { get; set; }

            public string ReasonPhrase { get; set; }

            public string Scheme { get; set; }

            public int StatusCode { get; set; }

            public ICollection<StringValues> Values => null;

            public void Add(KeyValuePair<string, StringValues> item) { }

            public void Add(string key, StringValues value) { }

            public void Clear() { }

            public bool Contains(KeyValuePair<string, StringValues> item) => false;

            public bool ContainsKey(string key) => false;

            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) { }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => null;

            public void OnCompleted(Func<object, Task> callback, object state) { }

            public void OnStarting(Func<object, Task> callback, object state) { }

            public bool Remove(KeyValuePair<string, StringValues> item) => false;

            public bool Remove(string key) => false;

            public bool TryGetValue(string key, out StringValues value)
            {
                value = StringValues.Empty;
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => null;
        }

        private class StubHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
        {
            public string TraceIdentifier { get; set; }
        }
    }
}