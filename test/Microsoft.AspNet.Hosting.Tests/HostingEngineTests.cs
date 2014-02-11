using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Server;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class HostingEngineTests : IServerManager, IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineCanBeResolvedWithDefaultServices()
        {
            var services = new ServiceProvider()
                .Add(HostingServices.GetDefaultServices());

            var engine = services.GetService<IHostingEngine>();

            Assert.NotNull(engine);
        }

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var services = new ServiceProvider()
                .Add(HostingServices.GetDefaultServices()
                    .Where(descriptor => descriptor.ServiceType != typeof(IServerManager)))
                .AddInstance<IServerManager>(this);

            var engine = services.GetService<IHostingEngine>();

            var context = new HostingContext
            {
                ApplicationName = "Microsoft.AspNet.Hosting.Tests.Fakes.FakeStartup, Microsoft.AspNet.Hosting.Tests"
            };

            var engineStart = engine.Start(context);

            Assert.NotNull(engineStart);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engineStart.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        public IServerFactory GetServer(string serverName)
        {
            return this;
        }

        public void Initialize(IBuilder builder)
        {

        }

        public IDisposable Start(Func<object, Task> application)
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
