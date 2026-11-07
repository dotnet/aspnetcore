// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.Microbenchmarks;

[MemoryDiagnoser]
public class OutputCacheMiddlewareBenchmarks
{
    private static readonly byte[] _body = "Hello World!"u8.ToArray();

    private OutputCacheMiddleware _middleware = default!;
    private Endpoint _endpointWithPolicy = default!;

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(builder => builder.Cache());

        _middleware = new OutputCacheMiddleware(
            context => context.Response.Body.WriteAsync(_body).AsTask(),
            Options.Create(options),
            NullLoggerFactory.Instance,
            new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 * 1024 * 1024 })),
            new DefaultObjectPoolProvider(),
            new DefaultOutputCachePolicyProvider(Options.Create(options)));

        _endpointWithPolicy = new Endpoint(
            requestDelegate: null,
            new EndpointMetadataCollection(new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(5)).Build()),
            "WithPolicy");

        // Populate the cache.
        await BasePolicyCacheHit();
        await EndpointPolicyCacheHit();
    }

    private static HttpContext CreateContext(string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5001);
        httpContext.Request.Path = path;
        httpContext.Response.Body = Stream.Null;
        return httpContext;
    }

    [Benchmark]
    public Task BasePolicyCacheHit()
    {
        return _middleware.Invoke(CreateContext("/base"));
    }

    [Benchmark]
    public Task EndpointPolicyCacheHit()
    {
        var httpContext = CreateContext("/endpoint");
        httpContext.SetEndpoint(_endpointWithPolicy);
        return _middleware.Invoke(httpContext);
    }
}
