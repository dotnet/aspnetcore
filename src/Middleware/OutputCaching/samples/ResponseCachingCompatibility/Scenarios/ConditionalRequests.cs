// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// The cached response has an ETag and a Last-Modified header. Conditional requests matching them are answered with a
/// 304 from the cache.
/// </summary>
public sealed class ConditionalRequests : Scenario
{
    private static readonly DateTimeOffset LastModified = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override string Description => "If-None-Match and If-Modified-Since against a cached response";

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
        app.MapGet("/", (HttpContext context) =>
        {
            var headers = context.Response.GetTypedHeaders();
            context.Response.Headers.CacheControl = "public, max-age=10";
            headers.ETag = new("\"v1\"");
            headers.LastModified = LastModified;
            return $"response #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. First request: endpoint runs"),
        new("2. If-None-Match: \"v1\": 304 from cache", ConfigureRequest: Header("If-None-Match", "\"v1\"")),
        new("3. If-None-Match: \"v2\": served from cache", ConfigureRequest: Header("If-None-Match", "\"v2\"")),
        new("4. If-Modified-Since later: 304 from cache", ConfigureRequest: Header("If-Modified-Since", LastModified.AddDays(1).ToString("R"))),
        new("5. If-Modified-Since earlier: served from cache", ConfigureRequest: Header("If-Modified-Since", LastModified.AddDays(-1).ToString("R"))),
    ];

    private static Action<System.Net.Http.HttpRequestMessage> Header(string name, string value)
        => request => request.Headers.TryAddWithoutValidation(name, value);
}
