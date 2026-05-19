// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Castle.Core.Internal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheAttributeTests
{
    [Fact]
    public void Attribute_CreatesDefaultPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.Default));
        var policy = attribute.BuildPolicy();

        Assert.Equal(DefaultPolicy.Instance, policy);
    }

    [Fact]
    public async Task Attribute_CreatesExpirePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.Duration));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(42, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task Attribute_CreatesNoStorePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.NoStore));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task Attribute_CreatesNamedPolicy()
    {
        var options = new OutputCacheOptions();
        options.AddPolicy("MyPolicy", b => b.Expire(TimeSpan.FromSeconds(42)));

        var context = TestUtils.CreateUninitializedContext(options: options);

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.PolicyName));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(42, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task Attribute_NamedPolicyDoesNotInjectDefaultPolicy()
    {
        var options = new OutputCacheOptions();
        options.AddPolicy("MyPolicy", b => b.With(x => false).Cache());

        var context = TestUtils.CreateUninitializedContext(options: options);

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.PolicyName));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByHeaderPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByHeaderNames));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("HeaderA", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.Contains("HeaderC", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.DoesNotContain("HeaderB", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByQueryPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByQueryKeys));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("QueryA", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.Contains("QueryC", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.DoesNotContain("QueryB", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByRoutePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByRouteValueNames));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValueNames);
    }

    [Fact]
    public async Task Attribute_CreatesTagsPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.Tags));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("Tag1", (IEnumerable<string>)context.Tags);
        Assert.Contains("Tag2", (IEnumerable<string>)context.Tags);
    }

    private class OutputCacheMethods
    {
        public static OutputCacheAttribute GetAttribute(string methodName)
        {
            return typeof(OutputCacheMethods).GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetAttribute<OutputCacheAttribute>();
        }

        [OutputCache()]
        public static void Default() { }

        [OutputCache(Duration = 42)]
        public static void Duration() { }

        [OutputCache(NoStore = true)]
        public static void NoStore() { }

        [OutputCache(PolicyName = "MyPolicy")]
        public static void PolicyName() { }

        [OutputCache(VaryByHeaderNames = new[] { "HeaderA", "HeaderC" })]
        public static void VaryByHeaderNames() { }

        [OutputCache(VaryByQueryKeys = new[] { "QueryA", "QueryC" })]
        public static void VaryByQueryKeys() { }

        [OutputCache(VaryByRouteValueNames = new[] { "RouteA", "RouteC" })]
        public static void VaryByRouteValueNames() { }

        [OutputCache(Tags = new[] { "Tag1", "Tag2" })]
        public static void Tags() { }
    }
}
