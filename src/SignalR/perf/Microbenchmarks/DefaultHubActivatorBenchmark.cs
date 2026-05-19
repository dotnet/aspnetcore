// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

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
