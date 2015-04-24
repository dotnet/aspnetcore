// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime.Infrastructure;
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

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));
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

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));
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
        public void EnvDefaultsToDevelopmentIfNoConfig()
        {
            var engine = CreateBuilder().Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Development", env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToDevelopmentConfigValueIfSpecifiedWithOldKey()
        {
            var vals = new Dictionary<string, string>
            {
                { "ASPNET_ENV", "Staging" }
            };

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));

            var engine = CreateBuilder(config).Build();
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToDevelopmentConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "Hosting:Environment", "Staging" }
            };

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));

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
                Assert.True(env.IsEnvironment("Development"));
                Assert.True(env.IsEnvironment("developMent"));
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
        [OSSkipCondition(OperatingSystems.Unix | OperatingSystems.MacOSX)]
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
            Assert.IsType<DefaultRequestIdentifierFeature>(httpContext.GetFeature<IRequestIdentifierFeature>());
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
            var featuresSupportedByHost = new Mock<IFeatureCollection>();
            featuresSupportedByHost
                .Setup(fc => fc.Add(It.IsAny<Type>(), It.IsAny<object>()))
                .Throws(new NotImplementedException());
            featuresSupportedByHost
                .Setup(fc => fc.Add(new KeyValuePair<Type, object>(It.IsAny<Type>(), It.IsAny<object>())))
                .Throws(new NotImplementedException());
            _featuresSupportedByThisHost = featuresSupportedByHost.Object;

            var hostingEngine = CreateHostingEngine(requestDelegate);

            // Act
            var disposable = hostingEngine.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.IsType<DefaultRequestIdentifierFeature>(httpContext.GetFeature<IRequestIdentifierFeature>());
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
            var requestIdentifierFeature = new Mock<IRequestIdentifierFeature>().Object;
            _featuresSupportedByThisHost.Add(typeof(IRequestIdentifierFeature), requestIdentifierFeature);
            var hostingEngine = CreateHostingEngine(requestDelegate);

            // Act
            var disposable = hostingEngine.Start();

            // Assert
            Assert.NotNull(httpContext);
            Assert.Same(requestIdentifierFeature, httpContext.GetFeature<IRequestIdentifierFeature>());
        }

        private IHostingEngine CreateHostingEngine(RequestDelegate requestDelegate)
        {
            var applicationBuilder = new Mock<IApplicationBuilder>();
            applicationBuilder.Setup(appBuilder => appBuilder.Build()).Returns(requestDelegate);
            var applicationBuilderFactory = new Mock<IApplicationBuilderFactory>();
            applicationBuilderFactory
                .Setup(abf => abf.CreateBuilder(It.IsAny<object>()))
                .Returns(applicationBuilder.Object);

            var host = CreateBuilder()
                .UseServer(this)
                .UseServices(s =>
                {
                    s.AddInstance(applicationBuilderFactory.Object);
                    s.AddInstance(new Mock<ILogger<HostingEngine>>().Object);
                    s.AddSingleton<IHttpContextFactory, HttpContextFactory>();
                    s.AddInstance(new Mock<IHttpContextAccessor>().Object);
                    s.AddInstance(new Mock<IHostingEnvironment>().Object);
                })
                .UseStartup(
                    appBuilder => { },
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
            return new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider, config ?? new Configuration());
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
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
    }
}
