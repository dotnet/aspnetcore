// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// Concurrent requests arrive while the cache is empty. Response Caching doesn't lock, so the endpoint runs for each of them.
/// </summary>
/// <remarks>
/// The default Output Caching policy locks: the endpoint runs once and the other requests receive its response.
/// </remarks>
public sealed class ConcurrentRequests : Scenario
{
    public override string Description => "5 concurrent requests while the cache is empty (Response Caching doesn't lock)";

    // Before: Response Caching.
    public override WebApplication CreateResponseCachingApp(WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCaching();

        var app = builder.Build();
        app.UseResponseCaching();
        MapEndpoints(app);
        return app;
    }

    // After: Output Caching with the Response Caching semantics. Only the two caching lines change.
    public override WebApplication CreateOutputCachingApp(WebApplicationBuilder builder)
    {
        builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.UseResponseCachingSemantics()));

        var app = builder.Build();
        app.UseOutputCache();
        MapEndpoints(app);
        return app;
    }

    // The endpoint is slow, so the requests overlap.
    public override void MapEndpoints(WebApplication app)
    {
        var counter = 0;
        app.MapGet("/", async (HttpContext context) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            context.Response.Headers.CacheControl = "public, max-age=10";
            return $"response #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. 5 concurrent requests, empty cache", Parallel: 5),
        new("2. 5 concurrent requests, served from cache", Parallel: 5),
    ];
}
