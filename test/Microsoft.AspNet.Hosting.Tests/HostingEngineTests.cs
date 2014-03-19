using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Hosting.Server;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineCanBeResolvedWithDefaultServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            var services = serviceCollection.BuildServiceProvider();

            var engine = services.GetService<IHostingEngine>();

            Assert.NotNull(engine);
        }

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            var services = serviceCollection.BuildServiceProvider();

            var engine = services.GetService<IHostingEngine>();

            var context = new HostingContext
            {
                ServerFactory = this,
                ApplicationName = "Microsoft.AspNet.Hosting.Fakes.FakeStartup, Microsoft.AspNet.Hosting.Tests"
            };

            var engineStart = engine.Start(context);

            Assert.NotNull(engineStart);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engineStart.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        public void Initialize(IBuilder builder)
        {

        }

        public IServerInformation Initialize(IConfiguration configuraiton)
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
