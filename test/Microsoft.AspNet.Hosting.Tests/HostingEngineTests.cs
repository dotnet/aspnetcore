// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var context = new HostingContext
            {
                ServerFactory = this,
                ApplicationName = "Microsoft.AspNet.Hosting.Tests"
            };

            var engineStart = new HostingEngine().Start(context);

            Assert.NotNull(engineStart);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engineStart.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void ApplicationNameDefaultsToApplicationEnvironmentName()
        {
            var context = new HostingContext
            {
                ServerFactory = this
            };

            var engine = new HostingEngine();

            using (engine.Start(context))
            {
                Assert.Equal("Microsoft.AspNet.Hosting.Tests", context.ApplicationName);
            }
        }

        [Fact]
        public void EnvDefaultsToDevelopmentIfNoConfig()
        {
            var context = new HostingContext
            {
                ServerFactory = this
            };

            var engine = new HostingEngine();

            using (engine.Start(context))
            {
                Assert.Equal("Development", context.EnvironmentName);
                var env = context.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal("Development", env.EnvironmentName);
            }
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

            var context = new HostingContext
            {
                ServerFactory = this,
                Configuration = config
            };

            var engine = new HostingEngine();

            using (engine.Start(context))
            {
                Assert.Equal("Staging", context.EnvironmentName);
                var env = context.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal("Staging", env.EnvironmentName);
            }
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheProjectJson()
        {
            var context = new HostingContext
            {
                ServerFactory = this
            };

            var engineStart = new HostingEngine().Start(context);
            var env = context.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var context = new HostingContext
            {
                ServerFactory = this
            };

            var engine = new HostingEngine();

            using (engine.Start(context))
            {
                var env = context.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment("Development"));
                Assert.True(env.IsEnvironment("developMent"));
            }
        }

        public void Initialize(IApplicationBuilder builder)
        {

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
    }
}
