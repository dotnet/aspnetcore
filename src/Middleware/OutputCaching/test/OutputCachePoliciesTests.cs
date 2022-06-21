// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.AspNetCore.OutputCaching.Policies;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCachePoliciesTests
{
    [Fact]
    public async Task DefaultCachePolicy_EnablesCache()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.True(context.EnableOutputCaching);
    }

    [Fact]
    public async Task DefaultCachePolicy_AllowsLocking()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task DefaultCachePolicy_VariesByStar()
    {
        IOutputCachePolicy policy = DefaultPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.Equal("*", context.CachedVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task EnableCachePolicy_DisablesCache()
    {
        IOutputCachePolicy policy = EnableCachePolicy.Disabled;
        var context = TestUtils.CreateUninitializedContext();
        context.EnableOutputCaching = true;

        await policy.CacheRequestAsync(context);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task ExpirationPolicy_SetsResponseExpirationTimeSpan()
    {
        var duration = TimeSpan.FromDays(1);
        IOutputCachePolicy policy = new ExpirationPolicy(duration);
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.Equal(duration, context.ResponseExpirationTimeSpan);
    }

    [Fact]
    public async Task LockingPolicy_EnablesLocking()
    {
        IOutputCachePolicy policy = LockingPolicy.Enabled;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.True(context.AllowLocking);
    }

    [Fact]
    public async Task LockingPolicy_DisablesLocking()
    {
        IOutputCachePolicy policy = LockingPolicy.Disabled;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.False(context.AllowLocking);
    }

    [Fact]
    public async Task NoLookupPolicy_DisablesLookup()
    {
        IOutputCachePolicy policy = NoLookupPolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

        Assert.False(context.AllowCacheLookup);
    }

    [Fact]
    public async Task NoStorePolicy_DisablesStore()
    {
        IOutputCachePolicy policy = NoStorePolicy.Instance;
        var context = TestUtils.CreateUninitializedContext();

        await policy.CacheRequestAsync(context);

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

        await predicate.CacheRequestAsync(context);

        Assert.Equal(expected, context.EnableOutputCaching);
    }

    [Fact]
    public async Task ProfilePolicy_UsesNamedProfile()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.Options.AddPolicy("enabled", EnableCachePolicy.Enabled);
        context.Options.AddPolicy("disabled", EnableCachePolicy.Disabled);

        IOutputCachePolicy policy = new ProfilePolicy("enabled");

        await policy.CacheRequestAsync(context);

        Assert.True(context.EnableOutputCaching);

        policy = new ProfilePolicy("disabled");

        await policy.CacheRequestAsync(context);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task TagsPolicy_Tags()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new TagsPolicy("tag1", "tag2");

        await policy.CacheRequestAsync(context);

        Assert.Contains("tag1", context.Tags);
        Assert.Contains("tag2", context.Tags);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_IsEmpty()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new VaryByHeaderPolicy();

        await policy.CacheRequestAsync(context);

        Assert.Empty(context.CachedVaryByRules.Headers);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_AddsSingleHeader()
    {
        var context = TestUtils.CreateUninitializedContext();
        var header = "header";

        IOutputCachePolicy policy = new VaryByHeaderPolicy(header);

        await policy.CacheRequestAsync(context);

        Assert.Equal(header, context.CachedVaryByRules.Headers);
    }

    [Fact]
    public async Task VaryByHeadersPolicy_AddsMultipleHeaders()
    {
        var context = TestUtils.CreateUninitializedContext();
        var headers = new[] { "header1", "header2" };

        IOutputCachePolicy policy = new VaryByHeaderPolicy(headers);

        await policy.CacheRequestAsync(context);

        Assert.Equal(headers, context.CachedVaryByRules.Headers);
    }

    [Fact]
    public async Task VaryByQueryPolicy_IsEmpty()
    {
        var context = TestUtils.CreateUninitializedContext();

        IOutputCachePolicy policy = new VaryByQueryPolicy();

        await policy.CacheRequestAsync(context);

        Assert.Empty(context.CachedVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task VaryByQueryPolicy_AddsSingleHeader()
    {
        var context = TestUtils.CreateUninitializedContext();
        var query = "query";

        IOutputCachePolicy policy = new VaryByQueryPolicy(query);

        await policy.CacheRequestAsync(context);

        Assert.Equal(query, context.CachedVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task VaryByQueryPolicy_AddsMultipleHeaders()
    {
        var context = TestUtils.CreateUninitializedContext();
        var queries = new[] { "query1", "query2" };

        IOutputCachePolicy policy = new VaryByQueryPolicy(queries);

        await policy.CacheRequestAsync(context);

        Assert.Equal(queries, context.CachedVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task VaryByValuePolicy_SingleValue()
    {
        var context = TestUtils.CreateUninitializedContext();
        var value = "value";

        IOutputCachePolicy policy = new VaryByValuePolicy(context => value);

        await policy.CacheRequestAsync(context);

        Assert.Equal(value, context.CachedVaryByRules.VaryByPrefix);
    }

    [Fact]
    public async Task VaryByValuePolicy_SingleValueAsync()
    {
        var context = TestUtils.CreateUninitializedContext();
        var value = "value";

        IOutputCachePolicy policy = new VaryByValuePolicy((context, token) => ValueTask.FromResult(value));

        await policy.CacheRequestAsync(context);

        Assert.Equal(value, context.CachedVaryByRules.VaryByPrefix);
    }

    [Fact]
    public async Task VaryByValuePolicy_KeyValuePair()
    {
        var context = TestUtils.CreateUninitializedContext();
        var key = "key";
        var value = "value";

        IOutputCachePolicy policy = new VaryByValuePolicy(context => new KeyValuePair<string, string>(key, value));

        await policy.CacheRequestAsync(context);

        Assert.Contains(new KeyValuePair<string, string>(key, value), context.CachedVaryByRules.VaryByCustom);
    }

    [Fact]
    public async Task VaryByValuePolicy_KeyValuePairAsync()
    {
        var context = TestUtils.CreateUninitializedContext();
        var key = "key";
        var value = "value";

        IOutputCachePolicy policy = new VaryByValuePolicy((context, token) => ValueTask.FromResult(new KeyValuePair<string, string>(key, value)));

        await policy.CacheRequestAsync(context);

        Assert.Contains(new KeyValuePair<string, string>(key, value), context.CachedVaryByRules.VaryByCustom);
    }
}
