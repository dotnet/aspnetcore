// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// Clients control the freshness of the responses they accept with request directives: only-if-cached, min-fresh,
/// max-age, max-stale, and the legacy "Pragma: no-cache". The cached response has "Cache-Control: public, max-age=10".
/// </summary>
/// <remarks>
/// The default Output Caching policy ignores the request directives, except only-if-cached.
/// </remarks>
public sealed class RequestCacheDirectives : Scenario
{
    public override string Description => "Clients send only-if-cached, Pragma: no-cache, min-fresh, max-age and max-stale";

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

    public override void MapEndpoints(WebApplication app)
    {
        var counter = 0;
        app.MapGet("/{path?}", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=10";
            return $"response #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. First request: endpoint runs"),
        new("2. 'only-if-cached': served from cache", ConfigureRequest: CacheControl("only-if-cached")),
        new("3. 'Pragma: no-cache': endpoint runs", ConfigureRequest: request => request.Headers.TryAddWithoutValidation("Pragma", "no-cache")),
        new("4. 'min-fresh=20': not fresh enough, endpoint runs", ConfigureRequest: CacheControl("min-fresh=20")),
        new("5. Wait 2s, 'max-age=1': too old, endpoint runs", ConfigureRequest: CacheControl("max-age=1"), DelaySeconds: 2),
        new("6. Wait 2s, 'max-age=1, max-stale=5': served from cache", ConfigureRequest: CacheControl("max-age=1, max-stale=5"), DelaySeconds: 2),
        new("7. 'only-if-cached', nothing cached: 504", Path: "/other", ConfigureRequest: CacheControl("only-if-cached")),
    ];

    private static Action<System.Net.Http.HttpRequestMessage> CacheControl(string value)
        => request => request.Headers.TryAddWithoutValidation("Cache-Control", value);
}
