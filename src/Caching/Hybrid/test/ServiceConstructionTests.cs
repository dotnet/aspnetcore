// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class ServiceConstructionTests
{
    [Fact]
    public void CanCreateService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
    }

    [Fact]
    public async Task BasicStatelessUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), async _ => expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task BasicStatefulUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), expected, async (state, _) => state);
        Assert.Equal(expected, actual);
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
