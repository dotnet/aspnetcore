// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class ServiceConstructionTests
{
    [Fact]
    public void CanCreateDefaultService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
    }

    [Fact]
    public void CanCreateServiceWithManualOptions()
    {
        var services = new ServiceCollection();
        services.AddHybridCache(options =>
        {
            options.MaximumKeyLength = 937;
            options.DefaultEntryOptions = new() { Expiration = TimeSpan.FromSeconds(120), Flags = HybridCacheEntryFlags.DisableLocalCacheRead };
        });
        using var provider = services.BuildServiceProvider();
        var obj = Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
        var options = obj.Options;
        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.Expiration);
        Assert.Equal(HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Null(defaults.LocalCacheExpiration); // wasn't specified
    }

    [Fact]
    public void CanParseOptions_NoEntryOptions()
    {
        var source = new JsonConfigurationSource { Path = "BasicConfig.json" };
        var configBuilder = new ConfigurationBuilder { Sources = { source } };
        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "no_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        Assert.Null(options.DefaultEntryOptions);
    }
    [Fact]
    public void CanParseOptions_WithEntryOptions() // in particular, check we can parse the timespan and [Flags] enums
    {
        var source = new JsonConfigurationSource { Path = "BasicConfig.json" };
        var configBuilder = new ConfigurationBuilder { Sources = { source } };
        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "with_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(HybridCacheEntryFlags.DisableCompression | HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.LocalCacheExpiration);
        Assert.Null(defaults.Expiration); // wasn't specified
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
