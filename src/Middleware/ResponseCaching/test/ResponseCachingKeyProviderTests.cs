// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class ResponseCachingKeyProviderTests
{
    private static readonly char KeyDelimiter = '\x1e';
    private static readonly char KeySubDelimiter = '\x1f';

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageBaseKey_IncludesOnlyNormalizedMethodSchemeHostPortAndPath()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Method = "head";
        context.HttpContext.Request.Path = "/path/subpath";
        context.HttpContext.Request.Scheme = "https";
        context.HttpContext.Request.Host = new HostString("example.com", 80);
        context.HttpContext.Request.PathBase = "/pathBase";
        context.HttpContext.Request.QueryString = new QueryString("?query.Key=a&query.Value=b");

        Assert.Equal($"HEAD{KeyDelimiter}HTTPS{KeyDelimiter}EXAMPLE.COM:80/PATHBASE/PATH/SUBPATH", cacheKeyProvider.CreateBaseKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageBaseKey_CaseInsensitivePath_NormalizesPath()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new ResponseCachingOptions()
        {
            UseCaseSensitivePaths = false
        });
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Path = "/Path";

        Assert.Equal($"{HttpMethods.Get}{KeyDelimiter}{KeyDelimiter}/PATH", cacheKeyProvider.CreateBaseKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageBaseKey_CaseSensitivePath_PreservesPathCase()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new ResponseCachingOptions()
        {
            UseCaseSensitivePaths = true
        });
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Path = "/Path";

        Assert.Equal($"{HttpMethods.Get}{KeyDelimiter}{KeyDelimiter}/Path", cacheKeyProvider.CreateBaseKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryByKey_Throws_IfVaryByRulesIsNull()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();

        Assert.Throws<InvalidOperationException>(() => cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_ReturnsCachedVaryByGuid_IfVaryByRulesIsEmpty()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}", cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_IncludesListedHeadersOnly()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            Headers = new string[] { "HeaderA", "HeaderC" }
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC=",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_HeaderValuesAreSorted()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueB";
        context.HttpContext.Request.Headers.Append("HeaderA", "ValueA");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            Headers = new string[] { "HeaderA", "HeaderC" }
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueAValueB{KeyDelimiter}HeaderC=",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_IncludesListedQueryKeysOnly()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            QueryKeys = new string[] { "QueryA", "QueryC" }
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC=",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_IncludesQueryKeys_QueryKeyCaseInsensitive_UseQueryKeyCasing()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?queryA=ValueA&queryB=ValueB");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            QueryKeys = new string[] { "QueryA", "QueryC" }
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC=",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_IncludesAllQueryKeysGivenAsterisk()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            QueryKeys = new string[] { "*" }
        };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeyDelimiter}QUERYB=ValueB",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_QueryKeysValuesNotConsolidated()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryA=ValueB");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            QueryKeys = new string[] { "*" }
        };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeySubDelimiter}ValueB",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_QueryKeysValuesAreSorted()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueB&QueryA=ValueA");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            QueryKeys = new string[] { "*" }
        };

        // To support case insensitivity, all query keys are converted to upper case.
        // Explicit query keys uses the casing specified in the setting.
        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}QUERYA=ValueA{KeySubDelimiter}ValueB",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }

    [Fact]
    public void ResponseCachingKeyProvider_CreateStorageVaryKey_IncludesListedHeadersAndQueryKeys()
    {
        var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");
        context.CachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            Headers = new string[] { "HeaderA", "HeaderC" },
            QueryKeys = new string[] { "QueryA", "QueryC" }
        };

        Assert.Equal($"{context.CachedVaryByRules.VaryByKeyPrefix}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC={KeyDelimiter}Q{KeyDelimiter}QueryA=ValueA{KeyDelimiter}QueryC=",
            cacheKeyProvider.CreateStorageVaryByKey(context));
    }
}
