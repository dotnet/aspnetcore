// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// The endpoint controls caching with the Cache-Control response header, e.g. "Cache-Control: public, max-age=3",
/// which is also what [ResponseCache(Duration = 3)] writes in MVC. Clients can bypass the cache with request headers.
/// </summary>
public sealed class CacheControlHeaders : Scenario
{
    public override string Description => "Endpoint sets 'Cache-Control: public, max-age=3'; clients send 'no-cache' or 'max-age=0'";

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

    // The counter shows whether the endpoint ran or the response came from the cache.
    public override void MapEndpoints(WebApplication app)
    {
        var counter = 0;
        app.MapGet("/", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=3";
            return $"response #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. First request: the endpoint runs"),
        new("2. Same request within max-age: served from cache"),
        new("3. Client sends 'Cache-Control: no-cache': endpoint runs", ConfigureRequest: r => r.Headers.TryAddWithoutValidation("Cache-Control", "no-cache")),
        new("4. Client sends 'Cache-Control: max-age=0': endpoint runs", ConfigureRequest: r => r.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0")),
        new("5. Same request: served from cache"),
        new("6. Wait 4s, max-age=3 expired: endpoint runs", DelaySeconds: 4),
        new("7. Query string isn't part of the key: served from cache", Path: "/?page=2"),
    ];
}
