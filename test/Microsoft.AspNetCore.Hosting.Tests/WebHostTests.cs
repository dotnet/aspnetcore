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
using Microsoft.AspNetCore.Hosting.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public class WebHostTests : IServerFactory, IServer
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();
        private IFeatureCollection _featuresSupportedByThisHost = NewFeatureCollection();
        private IFeatureCollection _instanceFeaturesSupportedByThisHost;

        public IFeatureCollection Features
        {
            get
            {
                var features = new FeatureCollection();

                foreach (var feature in _featuresSupportedByThisHost)
                {
                    features[feature.Key] = feature.Value;
                }

                if (_instanceFeaturesSupportedByThisHost != null)
                {
                    foreach (var feature in _instanceFeaturesSupportedByThisHost)
                    {
                        features[feature.Key] = feature.Value;
                    }
                }

                return features;
            }
        }

        static IFeatureCollection NewFeatureCollection()
        {
            var stub = new StubFeatures();
            var features = new FeatureCollection();
            features[typeof(IHttpRequestFeature)] = stub;
            features[typeof(IHttpResponseFeature)] = stub;
            return features;
        }

        [Fact]
        public void WebHostThrowsWithNoServer()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => CreateBuilder().Build().Start());
            Assert.True(ex.Message.Contains("UseServer()"));
        }

        [Fact]
        public void UseStartupThrowsWithNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateBuilder().UseStartup((string)null));
        }

        [Fact]
        public void CanStartWithOldServerConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "server", "Microsoft.AspNetCore.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var host = CreateBuilder(config).Build();
            host.Start();
            Assert.NotNull(host.Services.GetService<IHostingEnvironment>());
        }

        [Fact]
        public void CanStartWithServerConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNetCore.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var host = CreateBuilder(config).Build();
            host.Start();
            Assert.NotNull(host.Services.GetService<IHostingEnvironment>());
        }

        [Fact]
        public void CanDefaultAddresseIfNotConfigured()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNetCore.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var host = CreateBuilder(config).Build();
            host.Start();
            Assert.NotNull(host.Services.GetService<IHostingEnvironment>());
            Assert.Equal("http://localhost:5000", host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void WebHostCanBeStarted()
        {
            var host = CreateBuilder()
                .UseServer((IServerFactory)this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Start();

            Assert.NotNull(host);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            host.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void WebHostShutsDownWhenTokenTriggers()
        {
            var host = CreateBuilder()
                .UseServer((IServerFactory)this)
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

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void WebHostDisposesServiceProvider()
        {
            var host = CreateBuilder()
                .UseServer((IServerFactory)this)
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
                .UseServer((IServerFactory)this)
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
        public void WebHostInjectsHostingEnvironment()
        {
            var host = CreateBuilder()
                .UseServer((IServerFactory)this)
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
                    services.AddTransient<IStartupLoader, TestLoader>();
                })
                .UseServer((IServerFactory)this)
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            var host = CreateBuilder().UseServer((IServerFactory)this).ConfigureServices(services => services.AddOptions()).Build();
            Assert.NotNull(host.Services.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void ConfiguresStartupFiltersInCorrectOrder()
        {
            // Verify ordering
            var configureOrder = 0;
            var host = CreateBuilder()
                .UseServer((IServerFactory)this)
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
            var host = CreateBuilder().UseServer((IServerFactory)this).Build();
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

            var host = CreateBuilder(config).UseServer((IServerFactory)this).Build();
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

            var host = CreateBuilder(config).UseServer((IServerFactory)this).Build();
            var env = host.Services.GetService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var host = CreateBuilder().UseServer((IServerFactory)this).Build();
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
                .UseServer((IServerFactory)this)
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
                .UseServer((IServerFactory)this)
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
                .UseServer((IServerFactory)this)
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

        public IServer CreateServer(IConfiguration configuration)
        {
            _instanceFeaturesSupportedByThisHost = new FeatureCollection();
            _instanceFeaturesSupportedByThisHost.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            return this;
        }

        private class StartInstance : IDisposable
        {
            public int DisposeCalls { get; set; }

            public void Dispose()
            {
                DisposeCalls += 1;
            }
        }

        private class TestLoader : IStartupLoader
        {
            public Type FindStartupType(string startupAssemblyName, IList<string> diagnosticMessages)
            {
                throw new NotImplementedException();
            }

            public StartupMethods LoadMethods(Type startupType, IList<string> diagnosticMessages)
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

        private class ServerAddressesFeature : IServerAddressesFeature
        {
            public ICollection<string> Addresses { get; } = new List<string>();
        }

        private class AllMessagesAreNeeded : ILoggerProvider, ILogger
        {
            public bool IsEnabled(LogLevel logLevel) => true;

            public ILogger CreateLogger(string name) => this;

            public IDisposable BeginScopeImpl(object state)
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
