// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheKeyProviderTests
{
    private const char KeyDelimiter = '\x1e';
    private const char KeySubDelimiter = '\x1f';
    private static readonly string EmptyBaseKey = $"{KeyDelimiter}{KeyDelimiter}";

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesOnlyNormalizedMethodSchemeHostPortAndPath()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Method = "head";
        context.HttpContext.Request.Path = "/path/subpath";
        context.HttpContext.Request.Scheme = "https";
        context.HttpContext.Request.Host = new HostString("example.com", 80);
        context.HttpContext.Request.PathBase = "/pathBase";
        context.HttpContext.Request.QueryString = new QueryString("?query.Key=a&query.Value=b");

        Assert.Equal($"HEAD{KeyDelimiter}HTTPS{KeyDelimiter}EXAMPLE.COM:80/PATHBASE/PATH/SUBPATH", cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IgnoresHost()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.CacheVaryByRules.VaryByHost = false;

        context.HttpContext.Request.Method = "head";
        context.HttpContext.Request.Path = "/path/subpath";
        context.HttpContext.Request.Scheme = "https";
        context.HttpContext.Request.Host = new HostString("example.com", 80);
        context.HttpContext.Request.PathBase = "/pathBase";
        context.HttpContext.Request.QueryString = new QueryString("?query.Key=a&query.Value=b");

        Assert.Equal($"HEAD{KeyDelimiter}HTTPS{KeyDelimiter}*:*/PATHBASE/PATH/SUBPATH", cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_CaseInsensitivePath_NormalizesPath()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new OutputCacheOptions()
        {
            UseCaseSensitivePaths = false
        });
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Path = "/Path";

        Assert.Equal($"{HttpMethods.Get}{KeyDelimiter}{KeyDelimiter}/PATH", cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_CaseSensitivePath_PreservesPathCase()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new OutputCacheOptions()
        {
            UseCaseSensitivePaths = true
        });
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Path = "/Path";

        Assert.Equal($"{HttpMethods.Get}{KeyDelimiter}{KeyDelimiter}/Path", cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_VaryByRulesIsotNull()
    {
        var context = TestUtils.CreateTestContext();

        Assert.NotNull(context.CacheVaryByRules);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_ReturnsCachedVaryByGuid_IfVaryByRulesIsEmpty()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}", cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesListedRouteValuesOnly()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.RouteValues["RouteA"] = "ValueA";
        context.HttpContext.Request.RouteValues["RouteB"] = "ValueB";
        context.CacheVaryByRules.RouteValueNames = new string[] { "RouteA", "RouteC" };

        Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}R{KeyDelimiter}RouteA=ValueA{KeyDelimiter}RouteC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_SerializeRouteValueToStringInvariantCulture()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.RouteValues["RouteA"] = 123.456;
        context.CacheVaryByRules.RouteValueNames = new string[] { "RouteA", "RouteC" };

        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}R{KeyDelimiter}RouteA=123.456{KeyDelimiter}RouteC=",
                cacheKeyProvider.CreateStorageKey(context));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_ValuesAreSorted()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.CacheVaryByRules.VaryByValues["b"] = "ValueB";
        context.CacheVaryByRules.VaryByValues["a"] = "ValueA";

        Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}V{KeyDelimiter}a=ValueA{KeyDelimiter}b=ValueB",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesListedHeadersOnly()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
        context.CacheVaryByRules.HeaderNames = new string[] { "HeaderA", "HeaderC" };

        Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UsesListedHeaderKey_AsKey()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.CacheVaryByRules.HeaderNames = new string[] { "HEADERA" };

        Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}H{KeyDelimiter}HEADERA=ValueA",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_HeaderValuesAreSorted()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueB";
        context.HttpContext.Request.Headers.Append("HeaderA", "ValueA");
        context.CacheVaryByRules.HeaderNames = new string[] { "HeaderA", "HeaderC" };

        Assert.Equal($"{EmptyBaseKey}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueAValueB{KeyDelimiter}HeaderC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesListedQueryKeysOnly()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "QueryA", "QueryC" };

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesQueryKeys_QueryKeyCaseInsensitive_UseQueryKeyCasing()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?queryA=ValueA&queryB=ValueB");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "QueryA", "QueryC" };

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UseListedQueryKeys_AsKey()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?queryA=ValueA");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "QUERYA" };

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesAllQueryKeysGivenAsterisk()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "*" };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeyDelimiter}QUERYB=ValueB",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_QueryKeysValuesNotConsolidated()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryA=ValueB");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "*" };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeySubDelimiter}ValueB",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_QueryKeysValuesAreSorted()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueB&QueryA=ValueA");
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.QueryKeys = new string[] { "*" };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeySubDelimiter}ValueB",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_IncludesListedHeadersAndQueryKeysAndRouteValues()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.HttpContext.Request.RouteValues["RouteA"] = "ValueA";
        context.HttpContext.Request.RouteValues["RouteB"] = "ValueB";
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.HeaderNames = new string[] { "HeaderA", "HeaderC" };
        context.CacheVaryByRules.QueryKeys = new string[] { "QueryA", "QueryC" };
        context.CacheVaryByRules.RouteValueNames = new string[] { "RouteA", "RouteC" };

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC={KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC={KeyDelimiter}R{KeyDelimiter}RouteA=ValueA{KeyDelimiter}RouteC=",
            cacheKeyProvider.CreateStorageKey(context));
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_PathCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Path = "/path" + KeyDelimiter;

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_HostCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();

        Assert.Throws<ArgumentException>(() =>
        {
            context.HttpContext.Request.Host = new HostString("example.com" + KeyDelimiter, 80);
            cacheKeyProvider.CreateStorageKey(context);
        });
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_PathBaseCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.PathBase = "/pathBase" + KeyDelimiter;

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_HeaderValuesCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA" + KeyDelimiter;
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
        context.CacheVaryByRules.HeaderNames = new string[] { "HeaderA", "HeaderC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UnlistedHeadersCanContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB" + KeyDelimiter;
        context.CacheVaryByRules.HeaderNames = new string[] { "HeaderA", "HeaderC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.NotEmpty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_QueryStringValueCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString($"?QueryA=ValueA{KeyDelimiter}&QueryB=ValueB");
        context.CacheVaryByRules.QueryKeys = new string[] { "QueryA", "QueryC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_QueryStringKeyCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString($"?QueryA{KeyDelimiter}=ValueA&QueryB=ValueB");
        context.CacheVaryByRules.QueryKeys = new string[] { "*" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UnlistedQueryStringCanContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString($"?QueryA=ValueA&QueryB=ValueB{KeyDelimiter}");
        context.CacheVaryByRules.QueryKeys = new string[] { "QueryA", "QueryC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.NotEmpty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_RouteValuesCantContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.RouteValues["RouteA"] = "ValueA" + KeyDelimiter;
        context.HttpContext.Request.RouteValues["RouteB"] = "ValueB";
        context.CacheVaryByRules.RouteValueNames = new string[] { "RouteA", "RouteC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.Empty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UnlistedRouteValuesCanContainDelimiter()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.RouteValues["RouteA"] = "ValueA";
        context.HttpContext.Request.RouteValues["RouteB"] = "ValueB" + KeyDelimiter;
        context.CacheVaryByRules.RouteValueNames = new string[] { "RouteA", "RouteC" };

        var cacheKey = cacheKeyProvider.CreateStorageKey(context);

        Assert.NotEmpty(cacheKey);
    }

    [Fact]
    public void OutputCachingKeyProvider_CreateStorageKey_UseListedRouteValueNames_AsKey()

    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.RouteValues["RouteA"] = "ValueA";
        context.CacheVaryByRules.CacheKeyPrefix = Guid.NewGuid().ToString("n");
        context.CacheVaryByRules.RouteValueNames = new string[] { "ROUTEA" };

        Assert.Equal($"{context.CacheVaryByRules.CacheKeyPrefix}{KeyDelimiter}{EmptyBaseKey}{KeyDelimiter}R{KeyDelimiter}ROUTEA=ValueA",
            cacheKeyProvider.CreateStorageKey(context));
    }
}
