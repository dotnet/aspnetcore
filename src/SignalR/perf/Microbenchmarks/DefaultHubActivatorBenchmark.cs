// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class DefaultHubActivatorBenchmark
    {
        private DefaultHubActivator<MyHub> _activator;
        private IServiceProvider _serviceProvider;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var services = new ServiceCollection();

            _activator = new DefaultHubActivator<MyHub>();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Benchmark]
        public int Create()
        {
            var handle = _activator.Create(_serviceProvider);
            var hub = handle.Hub;
            var result = hub.Addition();
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
