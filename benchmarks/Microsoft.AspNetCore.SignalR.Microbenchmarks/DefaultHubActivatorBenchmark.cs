using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class DefaultHubActivatorBenchmark
    {
        private DefaultHubActivator<MyHub> _activator;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var services = new ServiceCollection();

            _activator = new DefaultHubActivator<MyHub>(services.BuildServiceProvider());
        }

        [Benchmark]
        public int Create()
        {
            var hub = _activator.Create();
            int result = hub.Addition();
            return result;
        }

        public class MyHub : Hub
        {
            public int Addition()
            {
                return 1 + 1;
            }
        }
    }
}
