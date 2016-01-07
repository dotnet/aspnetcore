// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class WebApplicationTests : IServerFactory, IServer
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();
        private IFeatureCollection _featuresSupportedByThisHost = NewFeatureCollection();
        private IFeatureCollection _instanceFeaturesSupportedByThisHost;

        public IFeatureCollection Features {
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
        public void WebApplicationThrowsWithNoServer()
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
                { "server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var application = CreateBuilder(config).Build();
            application.Start();
            Assert.NotNull(application.Services.GetService<IHostingEnvironment>());
        }

        [Fact]
        public void CanStartWithServerConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var application = CreateBuilder(config).Build();
            application.Start();
            Assert.NotNull(application.Services.GetService<IHostingEnvironment>());
        }

        [Fact]
        public void CanSpecifyPortConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNet.Hosting.Tests" },
                { "HTTP_PLATFORM_PORT", "abc123" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var application = CreateBuilder(config).Build();
            application.Start();
            Assert.NotNull(application.Services.GetService<IHostingEnvironment>());
            Assert.Equal("http://localhost:abc123", application.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void CanDefaultAddresseIfNotConfigured()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var application = CreateBuilder(config).Build();
            application.Start();
            Assert.NotNull(application.Services.GetService<IHostingEnvironment>());
            Assert.Equal("http://localhost:5000", application.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
        }

        [Fact]
        public void FlowsConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "Server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var application = CreateBuilder(config).Build();
            application.Start();
            var hostingEnvironment = application.Services.GetService<IHostingEnvironment>();
            Assert.NotNull(hostingEnvironment.Configuration);
            Assert.Equal("Microsoft.AspNet.Hosting.Tests", hostingEnvironment.Configuration["Server"]);
        }

        [Fact]
        public void WebApplicationCanBeStarted()
        {
            var app = CreateBuilder()
                .UseServer((IServerFactory)this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Start();

            Assert.NotNull(app);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            app.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void WebApplicationDisposesServiceProvider()
        {
            var application = CreateBuilder()
                .UseServer((IServerFactory)this)
                .ConfigureServices(s =>
                {
                    s.AddTransient<IFakeService, FakeService>();
                    s.AddSingleton<IFakeSingletonService, FakeService>();
                })
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build();

            application.Start();

            var singleton = (FakeService)application.Services.GetService<IFakeSingletonService>();
            var transient = (FakeService)application.Services.GetService<IFakeService>();

            Assert.False(singleton.Disposed);
            Assert.False(transient.Disposed);

            application.Dispose();

            Assert.True(singleton.Disposed);
            Assert.True(transient.Disposed);
        }

        [Fact]
        public void WebApplicationNotifiesApplicationStarted()
        {
            var application = CreateBuilder()
                .UseServer((IServerFactory)this)
                .Build();
            var applicationLifetime = application.Services.GetService<IApplicationLifetime>();

            Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            using (application)
            {
                application.Start();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public void WebApplicationInjectsHostingEnvironment()
        {
            var application = CreateBuilder()
                .UseServer((IServerFactory)this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .UseEnvironment("WithHostingEnvironment")
                .Build();

            using (application)
            {
                application.Start();
                var env = application.Services.GetService<IHostingEnvironment>();
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
                .UseStartup("Microsoft.AspNet.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            var application = CreateBuilder().UseServer((IServerFactory)this).ConfigureServices(services => services.AddOptions()).Build();
            Assert.NotNull(application.Services.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            var application = CreateBuilder().UseServer((IServerFactory)this).Build();
            var env = application.Services.GetService<IHostingEnvironment>();
            Assert.Equal(EnvironmentName.Production, env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecifiedWithOldKey()
        {
            var vals = new Dictionary<string, string>
            {
                // Old key is actualy ASPNET_ENV but WebApplicationConfiguration expects environment
                // variable names stripped from ASPNET_ prefix so using just ENV here
                { "ENV", "Staging" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var application = CreateBuilder(config).UseServer((IServerFactory)this).Build();
            var env = application.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
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

            var application = CreateBuilder(config).UseServer((IServerFactory)this).Build();
            var env = application.Services.GetService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "webroot", "testroot" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var application = CreateBuilder(config).UseServer((IServerFactory)this).Build();
            var env = application.Services.GetService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var application = CreateBuilder().UseServer((IServerFactory)this).Build();
            using (application)
            {
                application.Start();
                var env = application.Services.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment(EnvironmentName.Production));
                Assert.True(env.IsEnvironment("producTion"));
            }
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("/", "/")]
        [InlineData(@"\", @"\")]
        [InlineData("sub", "sub")]
        [InlineData("sub/sub2/sub3", @"sub/sub2/sub3")]
        public void MapPath_Facts(string virtualPath, string expectedSuffix)
        {
            RunMapPath(virtualPath, expectedSuffix);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(@"sub/sub2\sub3\", @"sub/sub2/sub3/")]
        public void MapPath_Windows_Facts(string virtualPath, string expectedSuffix)
        {
            RunMapPath(virtualPath, expectedSuffix);
        }

        [Fact]
        public void WebApplication_CreatesDefaultRequestIdentifierFeature_IfNotPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
                {
                    httpContext = innerHttpContext;
                    return Task.FromResult(0);
                });
            var application = CreateApplication(requestDelegate);

            // Act
            application.Start();

            // Assert
            Assert.NotNull(httpContext);
            var featuresTraceIdentifier = httpContext.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
            Assert.False(string.IsNullOrWhiteSpace(httpContext.TraceIdentifier));
            Assert.Same(httpContext.TraceIdentifier, featuresTraceIdentifier);
        }

        [Fact]
        public void WebApplication_DoesNot_CreateDefaultRequestIdentifierFeature_IfPresent()
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
            var application = CreateApplication(requestDelegate);

            // Act
            application.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.Same(requestIdentifierFeature, httpContext.Features.Get<IHttpRequestIdentifierFeature>());
        }

        [Fact]
        public void WebApplication_InvokesConfigureMethodsOnlyOnce()
        {
            var application = CreateBuilder()
                .UseServer((IServerFactory)this)
                .UseStartup<CountStartup>()
                .Build();
            using (application)
            {
                application.Start();
                var services = application.Services;
                var services2 = application.Services;
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
        public void WebApplication_ThrowsForBadConfigureServiceSignature()
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

        private IWebApplication CreateApplication(RequestDelegate requestDelegate)
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

        private void RunMapPath(string virtualPath, string expectedSuffix)
        {
            var application = CreateBuilder().UseServer((IServerFactory)this).Build();

            using (application)
            {
                application.Start();
                var env = application.Services.GetRequiredService<IHostingEnvironment>();
                // MapPath requires webroot to be set, we don't care
                // about file provider so  just set it here
                env.WebRootPath = ".";
                var mappedPath = env.MapPath(virtualPath);
                expectedSuffix = expectedSuffix.Replace('/', Path.DirectorySeparatorChar);
                Assert.Equal(Path.Combine(env.WebRootPath, expectedSuffix), mappedPath);
            }
        }

        private IWebApplicationBuilder CreateBuilder(IConfiguration config = null)
        {
            return new WebApplicationBuilder().UseConfiguration(config ?? new ConfigurationBuilder().Build()).UseStartup("Microsoft.AspNet.Hosting.Tests");
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
            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
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
