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
public class MiddlewareBenchmarks
{
    private OutputCacheMiddleware _middleware = default!;
    private Endpoint _endpoint = default!;

    // Whether a base policy is configured in addition to the endpoint policy
    [Params(false, true)]
    public bool BasePolicy { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var options = new OutputCacheOptions();
        if (BasePolicy)
        {
            options.AddBasePolicy(b => b.Tag("tag"));
        }

        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { SizeLimit = options.SizeLimit }));
        var policy = new OutputCachePolicyBuilder().Build();
        _endpoint = new Endpoint(null, new EndpointMetadataCollection(policy), "test");

        _middleware = new OutputCacheMiddleware(
            static async context =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            },
            Options.Create(options),
            NullLoggerFactory.Instance,
            store,
            new DefaultObjectPoolProvider(),
            new DefaultOutputCachePolicyProvider(Options.Create(options)));

        // Populate the cache
        await _middleware.Invoke(CreateContext());
    }

    private HttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/hello";
        context.Response.Body = Stream.Null;
        context.SetEndpoint(_endpoint);
        return context;
    }

    [Benchmark]
    public Task CacheHit() => _middleware.Invoke(CreateContext());
}
