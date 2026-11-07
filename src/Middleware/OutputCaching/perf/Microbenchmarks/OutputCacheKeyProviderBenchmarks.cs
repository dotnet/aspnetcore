// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.Microbenchmarks;

[MemoryDiagnoser]
public class OutputCacheKeyProviderBenchmarks
{
    private OutputCacheKeyProvider _keyProvider = default!;
    private OutputCacheContext _defaultContext = default!;
    private OutputCacheContext _varyByQueryContext = default!;
    private OutputCacheContext _varyByHeaderContext = default!;

    [GlobalSetup]
    public void Setup()
    {
        _keyProvider = new OutputCacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(new OutputCacheOptions()));

        _defaultContext = CreateContext();

        _varyByQueryContext = CreateContext();
        _varyByQueryContext.HttpContext.Request.QueryString = new QueryString("?page=2&size=50&sort=name");
        _varyByQueryContext.CacheVaryByRules.QueryKeys = new[] { "page", "size" };

        _varyByHeaderContext = CreateContext();
        _varyByHeaderContext.HttpContext.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        _varyByHeaderContext.HttpContext.Request.Headers.AcceptEncoding = "gzip, deflate, br";
        _varyByHeaderContext.CacheVaryByRules.HeaderNames = new[] { "Accept-Language", "Accept-Encoding" };
    }

    private static OutputCacheContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("www.example.com", 5001);
        httpContext.Request.PathBase = "/store";
        httpContext.Request.Path = "/api/products/categories/42";

        return new OutputCacheContext { HttpContext = httpContext };
    }

    [Benchmark]
    public string CreateStorageKey_Default() => _keyProvider.CreateStorageKey(_defaultContext);

    [Benchmark]
    public string CreateStorageKey_VaryByQuery() => _keyProvider.CreateStorageKey(_varyByQueryContext);

    [Benchmark]
    public string CreateStorageKey_VaryByHeader() => _keyProvider.CreateStorageKey(_varyByHeaderContext);
}
