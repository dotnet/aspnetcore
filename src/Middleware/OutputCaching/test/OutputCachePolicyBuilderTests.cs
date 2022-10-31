// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCachePolicyBuilderTests
{
    [Fact]
    public void BuildPolicy_CreatesDefaultPolicy()
    {
        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Build();

        Assert.Equal(DefaultPolicy.Instance, policy);
    }

    [Fact]
    public void BuildPolicy_CreatedWithoutDefaultPolicy()
    {
        var builder = new OutputCachePolicyBuilder(true);
        var policy = builder.Build();

        Assert.Equal(EmptyPolicy.Instance, policy);
    }

    [Fact]
    public async Task BuildPolicy_CreatesExpirePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        var duration = 42;

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Expire(TimeSpan.FromSeconds(duration)).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(duration, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_CreatesNoStorePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_AddsCustomPolicy()
    {
        var options = new OutputCacheOptions();
        var name = "MyPolicy";
        var duration = 42;
        options.AddPolicy(name, b => b.Expire(TimeSpan.FromSeconds(duration)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.AddPolicy(new NamedPolicy(name)).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(duration, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_AddsCustomPolicyWithoutDefaultPolicy()
    {
        var options = new OutputCacheOptions();
        var name = "MyPolicy";
        var duration = 42;
        options.AddPolicy(name, b => b.Expire(TimeSpan.FromSeconds(duration)), true);

        var context = TestUtils.CreateUninitializedContext(options: options);

        var builder = new OutputCachePolicyBuilder(true);
        var policy = builder.AddPolicy(new NamedPolicy(name)).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
        Assert.Equal(duration, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_NoVaryByHost()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByHost(false).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.CacheVaryByRules.VaryByHost);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByHeaderPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByHeader("HeaderA", "HeaderC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("HeaderA", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.Contains("HeaderC", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.DoesNotContain("HeaderB", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByQueryPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByQuery("QueryA", "QueryC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("QueryA", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.Contains("QueryC", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.DoesNotContain("QueryB", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByRoutePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByRouteValue("RouteA", "RouteC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByRoutePolicyArray()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByRouteValue(new string[] { "RouteA", "RouteC" }).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByRoutePolicyReplacesValue()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetVaryByRouteValue("RouteB").SetVaryByRouteValue("RouteA", "RouteC").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByKeyPrefixPolicy()
    {
        var context1 = TestUtils.CreateUninitializedContext();
        var context2 = TestUtils.CreateUninitializedContext();
        var context3 = TestUtils.CreateUninitializedContext();

        var policy1 = new OutputCachePolicyBuilder().SetCacheKeyPrefix("tenant1").Build();
        var policy2 = new OutputCachePolicyBuilder().SetCacheKeyPrefix(context => "tenant2").Build();
        var policy3 = new OutputCachePolicyBuilder().SetCacheKeyPrefix((context, cancellationToken) => ValueTask.FromResult("tenant3")).Build();

        await policy1.CacheRequestAsync(context1, cancellation: default);
        await policy2.CacheRequestAsync(context2, cancellation: default);
        await policy3.CacheRequestAsync(context3, cancellation: default);

        Assert.Equal("tenant1", context1.CacheVaryByRules.CacheKeyPrefix);
        Assert.Equal("tenant2", context2.CacheVaryByRules.CacheKeyPrefix);
        Assert.Equal("tenant3", context3.CacheVaryByRules.CacheKeyPrefix);
    }

    [Fact]
    public async Task BuildPolicy_CreatesVaryByValuePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder
            .VaryByValue("shape", "circle")
            .VaryByValue(context => new KeyValuePair<string, string>("color", "blue"))
            .VaryByValue((context, cancellationToken) => ValueTask.FromResult(new KeyValuePair<string, string>("size", "1m")))
            .Build();

        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal("circle", context.CacheVaryByRules.VaryByValues["shape"]);
        Assert.Equal("blue", context.CacheVaryByRules.VaryByValues["color"]);
        Assert.Equal("1m", context.CacheVaryByRules.VaryByValues["size"]);
    }

    [Fact]
    public async Task BuildPolicy_CreatesTagPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Tag("tag1", "tag2").Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("tag1", context.Tags);
        Assert.Contains("tag2", context.Tags);
    }

    [Fact]
    public async Task BuildPolicy_AllowsLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_EnablesLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetLocking(true).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_DisablesLocking()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.SetLocking(false).Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.AllowLocking);
    }

    [Fact]
    public async Task BuildPolicy_ClearsDefaultPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder(true);
        var policy = builder.Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.AllowLocking);
        Assert.False(context.AllowCacheLookup);
        Assert.False(context.AllowCacheStorage);
        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_DisablesCache()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task BuildPolicy_EnablesCache()
    {
        var context = TestUtils.CreateUninitializedContext();

        var builder = new OutputCachePolicyBuilder();
        var policy = builder.NoCache().Cache().Build();
        await policy.CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task BuildPolicy_ChecksWithPredicate(int source, int expected)
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Expire(TimeSpan.FromSeconds(1)));
        options.AddBasePolicy(build => build.With(c => source == 1).Expire(TimeSpan.FromSeconds(2)));
        options.AddBasePolicy(build => build.With(c => source == 2).Expire(TimeSpan.FromSeconds(3)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(expected, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_NoDefaultWithFalsePredicate()
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.With(c => false).Expire(TimeSpan.FromSeconds(2)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.False(context.EnableOutputCaching);
        Assert.NotEqual(2, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task BuildPolicy_CacheReturnsDefault()
    {
        // Each predicate should override the duration from the first base policy
        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Cache());

        var context = TestUtils.CreateUninitializedContext(options: options);

        foreach (var policy in options.BasePolicies)
        {
            await policy.CacheRequestAsync(context, default);
        }

        Assert.True(context.EnableOutputCaching);
    }
}
