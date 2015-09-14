// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();
        private IFeatureCollection _featuresSupportedByThisHost = new FeatureCollection();

        [Fact]
        public void HostingEngineThrowsWithNoServer()
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
            var host = CreateBuilder(config).Build();
            host.Start();
            Assert.NotNull(host.ApplicationServices.GetRequiredService<IHostingEnvironment>());
        }

        [Fact]
        public void CanStartWithServerConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "Hosting:Server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();
            var host = CreateBuilder(config).Build();
            host.Start();
            Assert.NotNull(host.ApplicationServices.GetRequiredService<IHostingEnvironment>());
        }

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var engine = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build()
                .Start();

            Assert.NotNull(engine);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engine.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void HostingEngineNotifiesApplicationStarted()
        {
            var host = CreateBuilder()
                .UseServer(this)
                .Build();
            var applicationLifetime = host.ApplicationServices.GetRequiredService<IApplicationLifetime>();

            Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            using (host.Start())
            {
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public void HostingEngineInjectsHostingEnvironment()
        {
            var engine = CreateBuilder()
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .UseEnvironment("WithHostingEnvironment")
                .Build();

            using (var server = engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal("Changed", env.EnvironmentName);
            }
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var engine = CreateBuilder().UseServices(services => services.AddTransient<IStartupLoader, TestLoader>())
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Build();

            Assert.Throws<NotImplementedException>(() => engine.Start());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            var host = CreateBuilder().UseServices(services => services.AddOptions()).Build();
            Assert.NotNull(host.ApplicationServices.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            var engine = CreateBuilder().Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(EnvironmentName.Production, env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecifiedWithOldKey()
        {
            var vals = new Dictionary<string, string>
            {
                { "ASPNET_ENV", "Staging" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var engine = CreateBuilder(config).Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "Hosting:Environment", "Staging" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var engine = CreateBuilder(config).Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheProjectJson()
        {
            var engine = CreateBuilder().UseServer(this).Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var engine = CreateBuilder().UseServer(this).Build();
            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
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
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(@"sub/sub2\sub3\", @"sub/sub2/sub3/")]
        public void MapPath_Windows_Facts(string virtualPath, string expectedSuffix)
        {
            RunMapPath(virtualPath, expectedSuffix);
        }

        [Fact]
        public void HostingEngine_CreatesDefaultRequestIdentifierFeature_IfNotPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
                {
                    httpContext = innerHttpContext;
                    return Task.FromResult(0);
                });
            var hostingEngine = CreateHostingEngine(requestDelegate);

            // Act
            var disposable = hostingEngine.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.IsType<HttpRequestIdentifierFeature>(httpContext.Features.Get<IHttpRequestIdentifierFeature>());
        }

        [Fact]
        public void Hosting_CreatesDefaultRequestIdentifierFeature_IfNotPresent_ForImmutableFeatureCollection()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
            {
                httpContext = innerHttpContext;
                return Task.FromResult(0);
            });

            _featuresSupportedByThisHost = new ReadOnlyFeatureCollection();

            var hostingEngine = CreateHostingEngine(requestDelegate);

            // Act
            var disposable = hostingEngine.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.IsType<HttpRequestIdentifierFeature>(httpContext.Features.Get<IHttpRequestIdentifierFeature>());
        }

        [Fact]
        public void HostingEngine_DoesNot_CreateDefaultRequestIdentifierFeature_IfPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
            {
                httpContext = innerHttpContext;
                return Task.FromResult(0);
            });
            var requestIdentifierFeature = new Mock<IHttpRequestIdentifierFeature>().Object;
            _featuresSupportedByThisHost[typeof(IHttpRequestIdentifierFeature)] = requestIdentifierFeature;
            var hostingEngine = CreateHostingEngine(requestDelegate);

            // Act
            var disposable = hostingEngine.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.Same(requestIdentifierFeature, httpContext.Features.Get<IHttpRequestIdentifierFeature>());
        }

        [Fact]
        public void HostingEngine_InvokesConfigureMethodsOnlyOnce()
        {
            var engine = CreateBuilder()
                .UseServer(this)
                .UseStartup<CountStartup>()
                .Build();
            using (engine.Start())
            {
                var services = engine.ApplicationServices;
                var services2 = engine.ApplicationServices;
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
        public void HostingEngine_ThrowsForBadConfigureServiceSignature()
        {
            var engine = CreateBuilder()
                .UseServer(this)
                .UseStartup<BadConfigureServicesStartup>()
                .Build();
            var ex = Assert.Throws<InvalidOperationException>(() => engine.Start());
            Assert.True(ex.Message.Contains("ConfigureServices"));
        }

        public class BadConfigureServicesStartup
        {
            public void ConfigureServices(IServiceCollection services, int gunk) { }
            public void Configure(IApplicationBuilder app) { }
        }

        private IHostingEngine CreateHostingEngine(RequestDelegate requestDelegate)
        {
            var host = CreateBuilder()
                .UseServer(this)
                .UseStartup(
                    appBuilder => { appBuilder.Run(requestDelegate); },
                    configureServices => configureServices.BuildServiceProvider());
            return host.Build();
        }

        private void RunMapPath(string virtualPath, string expectedSuffix)
        {
            var engine = CreateBuilder().UseServer(this).Build();

            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                var mappedPath = env.MapPath(virtualPath);
                expectedSuffix = expectedSuffix.Replace('/', Path.DirectorySeparatorChar);
                Assert.Equal(Path.Combine(env.WebRootPath, expectedSuffix), mappedPath);
            }
        }

        private WebHostBuilder CreateBuilder(IConfiguration config = null)
        {
            return new WebHostBuilder(
                CallContextServiceLocator.Locator.ServiceProvider,
                config ?? new ConfigurationBuilder().Build());
        }

        public IFeatureCollection Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IFeatureCollection serverFeatures, Func<IFeatureCollection, Task> application)
        {
            var startInstance = new StartInstance(application);
            _startInstances.Add(startInstance);
            application(_featuresSupportedByThisHost);
            return startInstance;
        }

        public class StartInstance : IDisposable
        {
            private readonly Func<IFeatureCollection, Task> _application;

            public StartInstance(Func<IFeatureCollection, Task> application)
            {
                _application = application;
            }

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

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }
    }
}
