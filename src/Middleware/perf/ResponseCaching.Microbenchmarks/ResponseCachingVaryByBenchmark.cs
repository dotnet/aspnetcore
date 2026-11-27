// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Microbenchmarks;

[MemoryDiagnoser]
public class ResponseCachingVaryByBenchmark
{
    private static readonly string _cacheControl = $"{CacheControlHeaderValue.PublicString}, {CacheControlHeaderValue.MaxAgeString}={int.MaxValue}";
    private static readonly StringValues _varyHeaders = new(new[] { "Accept-Language", "Accept-Encoding" });
    private static readonly string[] _varyQueryKeys = new[] { "page", "size" };
    private static readonly byte[] _body = new byte[100];

    private ResponseCachingMiddleware _middleware = default!;

    [GlobalSetup]
    public void Setup()
    {
        _middleware = new ResponseCachingMiddleware(
            context =>
            {
                context.Features.Get<IResponseCachingFeature>()!.VaryByQueryKeys = _varyQueryKeys;
                context.Response.Headers.CacheControl = _cacheControl;
                context.Response.Headers.Vary = _varyHeaders;
                return context.Response.Body.WriteAsync(_body, 0, _body.Length);
            },
            Microsoft.Extensions.Options.Options.Create(new ResponseCachingOptions()),
            NullLoggerFactory.Instance,
            new DefaultObjectPoolProvider());

        // Populate the cache entry used by ServeFromCache.
        _middleware.Invoke(CreateHttpContext()).GetAwaiter().GetResult();
    }

    [Benchmark]
    public Task Cache()
    {
        var context = CreateHttpContext();
        // Bypass lookup but still store, exercising vary-by rule normalization and storage key creation.
        context.Request.Headers.CacheControl = CacheControlHeaderValue.NoCacheString;
        return _middleware.Invoke(context);
    }

    [Benchmark]
    public Task ServeFromCache() => _middleware.Invoke(CreateHttpContext());

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("www.example.com");
        context.Request.Path = "/api/products";
        context.Request.QueryString = new QueryString("?page=2&size=50&sort=name");
        context.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        context.Request.Headers.AcceptEncoding = "gzip, deflate, br";
        context.Response.Body = Stream.Null;
        return context;
    }
}
