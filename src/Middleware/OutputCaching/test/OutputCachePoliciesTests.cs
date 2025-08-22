// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching.Policies;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCachePoliciesTests
{
    [Fact]
    public async Task DefaultCachePolicy_EnablesCache()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.True(context.EnableOutputCaching);
    }

    [Fact]
    public async Task DefaultCachePolicy_VariesByHost()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.True(context.CacheVaryByRules.VaryByHost);
    }

    [Fact]
    public async Task DefaultCachePolicy_AllowsLocking()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task DefaultCachePolicy_VariesByStar()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.Equal("*", context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task EnableCachePolicy_DisablesCache()
    {
        IOutputCachePolicy policy = EnableCachePolicy.Disabled;
        var context = TestUtils.CreateUninitializedContext();
        context.EnableOutputCaching = true;

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task VaryByHostPolicy_Disabled_UpdatesCacheVaryByRule()
    {
        IOutputCachePolicy policy = VaryByHostPolicy.Disabled;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.CacheVaryByRules.VaryByHost);
    }

    [Fact]
    public async Task ExpirationPolicy_SetsResponseExpirationTimeSpan()
    {
        var duration = TimeSpan.FromDays(1);
        IOutputCachePolicy policy = new ExpirationPolicy(duration);
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(duration, context.ResponseExpirationTimeSpan);
    }

    [Fact]
    public async Task LockingPolicy_EnablesLocking()
    {
        IOutputCachePolicy policy = LockingPolicy.Enabled;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task LockingPolicy_DisablesLocking()
    {
        IOutputCachePolicy policy = LockingPolicy.Disabled;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.AllowLocking);
    }

    [Fact]
    public async Task NoLookupPolicy_DisablesLookup()
    {
        IOutputCachePolicy policy = NoLookupPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.AllowCacheLookup);
    }

    [Fact]
    public async Task NoStorePolicy_DisablesStore()
    {
        IOutputCachePolicy policy = NoStorePolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.AllowCacheStorage);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public async Task PredicatePolicy_Filters(bool filter, bool enabled, bool expected)
    {
        IOutputCachePolicy predicate = new PredicatePolicy(c => ValueTask.FromResult(filter), enabled ? EnableCachePolicy.Enabled : EnableCachePolicy.Disabled);
        var context = TestUtils.CreateUninitializedContext();

        await predicate.CacheRequestAsync(context, default);

        Assert.Equal(expected, context.EnableOutputCaching);
    }

    [Fact]
    public async Task ProfilePolicy_UsesNamedProfile()
    {
        var options = new OutputCacheOptions();
        options.AddPolicy("enabled", EnableCachePolicy.Enabled);
        options.AddPolicy("disabled", EnableCachePolicy.Disabled);
        var context = TestUtils.CreateUninitializedContext(options: options);

        IOutputCachePolicy policy = new NamedPolicy("enabled");

        await policy.CacheRequestAsync(context, default);

        Assert.True(context.EnableOutputCaching);

        policy = new NamedPolicy("disabled");

        await policy.CacheRequestAsync(context, default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task TagsPolicy_Tags()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new TagsPolicy("tag1", "tag2");

        await policy.CacheRequestAsync(context, default);

        Assert.Contains("tag1", context.Tags);
        Assert.Contains("tag2", context.Tags);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_IsEmpty()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new VaryByHeaderPolicy(Array.Empty<string>());

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(0, context.CacheVaryByRules.HeaderNames.Count);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_AddsSingleHeader()
    {
        var context = TestUtils.CreateUninitializedContext();
        var header = "header";

        IOutputCachePolicy policy = new VaryByHeaderPolicy(header);

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(header, context.CacheVaryByRules.HeaderNames);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_AddsMultipleHeaders()
    {
        var context = TestUtils.CreateUninitializedContext();
        var headers = new[] { "header1", "header2" };

        IOutputCachePolicy policy = new VaryByHeaderPolicy(headers);

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(headers, context.CacheVaryByRules.HeaderNames.ToArray());
    }

    [Fact]
    public async Task VaryByQueryPolicy_IsEmpty()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new VaryByQueryPolicy(Array.Empty<string>());

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(0, context.CacheVaryByRules.QueryKeys.Count);
    }

    [Fact]
    public async Task VaryByQueryPolicy_AddsSingleHeader()
    {
        var context = TestUtils.CreateUninitializedContext();
        var query = "query";

        IOutputCachePolicy policy = new VaryByQueryPolicy(query);

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(query, context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task VaryByQueryPolicy_AddsMultipleHeaders()
    {
        var context = TestUtils.CreateUninitializedContext();
        var queries = new[] { "query1", "query2" };

        IOutputCachePolicy policy = new VaryByQueryPolicy("query1", "query2");

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(queries, context.CacheVaryByRules.QueryKeys.ToArray());
    }

    [Fact]
    public async Task VaryByQueryPolicy_AddsMultipleHeadersArray()
    {
        var context = TestUtils.CreateUninitializedContext();
        var queries = new[] { "query1", "query2" };

        IOutputCachePolicy policy = new VaryByQueryPolicy(queries);

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(queries, context.CacheVaryByRules.QueryKeys.ToArray());
    }

    [Fact]
    public async Task VaryByKeyPrefixPolicy_AddsKeyPrefix()
    {
        var context = TestUtils.CreateUninitializedContext();
        var value = "value";

        IOutputCachePolicy policy = new SetCacheKeyPrefixPolicy((context, cancellationToken) => ValueTask.FromResult(value));

        await policy.CacheRequestAsync(context, default);

        Assert.Equal(value, context.CacheVaryByRules.CacheKeyPrefix);
    }

    [Fact]
    public async Task VaryByValuePolicy_KeyValuePair()
    {
        var context = TestUtils.CreateUninitializedContext();
        var key = "key";
        var value = "value";

        IOutputCachePolicy policy = new VaryByValuePolicy((context, CancellationToken) => ValueTask.FromResult(new KeyValuePair<string, string>(key, value)));

        await policy.CacheRequestAsync(context, default);

        Assert.Contains(new KeyValuePair<string, string>(key, value), context.CacheVaryByRules.VaryByValues);
    }
}
