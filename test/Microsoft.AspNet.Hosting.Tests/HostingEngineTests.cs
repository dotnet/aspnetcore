// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineCanBeResolvedWithDefaultServices()
        {
            var services = HostingServices.Create().BuildServiceProvider();

            var engine = services.GetRequiredService<IHostingEngine>();

            Assert.NotNull(engine);
        }

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var services = HostingServices.Create().BuildServiceProvider();

            var engine = services.GetRequiredService<IHostingEngine>();

            var context = new HostingContext
            {
                ServerFactory = this,
                Services = services,
                ApplicationName = "Microsoft.AspNet.Hosting.Tests"
            };

            var engineStart = engine.Start(context);

            Assert.NotNull(engineStart);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engineStart.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        public void Initialize(IApplicationBuilder builder)
        {

        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<object, Task> application)
        {
            var startInstance = new StartInstance(application);
            _startInstances.Add(startInstance);
            return startInstance;
        }

        public class StartInstance : IDisposable
        {
            private readonly Func<object, Task> _application;

            public StartInstance(Func<object, Task> application)
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
