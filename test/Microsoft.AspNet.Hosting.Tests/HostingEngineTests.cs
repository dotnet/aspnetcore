// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineThrowsWithNoServer()
        {
            Assert.Throws<InvalidOperationException>(() => WebHost.CreateEngine().Start());
        }

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var engine = WebHost.CreateEngine()
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Start();

            Assert.NotNull(engine);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engine.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void CanReplaceHostingFactory()
        {
            var factory = WebHost.CreateFactory(services => services.AddTransient<IHostingFactory, TestEngineFactory>());

            Assert.NotNull(factory as TestEngineFactory);
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var engine = WebHost.CreateEngine(services => services.AddTransient<IStartupLoader, TestLoader>())
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => engine.Start());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            var engineStart = WebHost.CreateEngine(services => services.AddOptions());
            Assert.NotNull(engineStart.ApplicationServices.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void EnvDefaultsToDevelopmentIfNoConfig()
        {
            var engine = WebHost.CreateEngine(new Configuration());
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Development", env.EnvironmentName);
        }

        [Fact]
        public void EnvDefaultsToDevelopmentConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "ASPNET_ENV", "Staging" }
            };

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));

            var engine = WebHost.CreateEngine(config);
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal("Staging", env.EnvironmentName);
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheProjectJson()
        {
            var engine = WebHost.CreateEngine().UseServer(this);
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var engine = WebHost.CreateEngine().UseServer(this);

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
        [InlineData(@"sub/sub2\sub3\", @"sub/sub2/sub3/")]
        public void MapPath_Facts(string virtualPath, string expectedSuffix)
        {
            var engine = WebHost.CreateEngine().UseServer(this);

            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                var mappedPath = env.MapPath(virtualPath);
                expectedSuffix = expectedSuffix.Replace('/', Path.DirectorySeparatorChar);
                Assert.Equal(Path.Combine(env.WebRootPath, expectedSuffix), mappedPath);
            }
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
        {
            var startInstance = new StartInstance(application);
            _startInstances.Add(startInstance);
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
            public StartupMethods Load(string startupAssemblyName, string environmentName, IList<string> diagnosticMessages)
            {
                throw new NotImplementedException();
            }
        }

        private class TestEngineFactory : IHostingFactory
        {
            public IHostingEngine Create(IConfiguration config)
            {
                throw new NotImplementedException();
            }
        }
    }
}
