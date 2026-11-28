// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// The caching middleware runs before Response Compression, so the cache stores compressed responses. Response Compression
/// adds "Vary: Accept-Encoding", so one response is cached per encoding.
/// </summary>
/// <remarks>
/// For requests which don't accept compression, Response Compression adds the Vary header when the response starts,
/// after the caching middleware read it. That response is then cached for every encoding, until it expires.
/// The default Output Caching policy ignores the Vary header, so it serves the first encoding to every client, even to
/// clients which don't accept it.
/// </remarks>
public sealed class ResponseCompression : Scenario
{
    public override string Description => "Response Compression adds 'Vary: Accept-Encoding'; compressed responses are cached";

    // Before: Response Caching.
    public override WebApplication CreateResponseCachingApp(WebApplicationBuilder builder)
    {
        ConfigureServices(builder.Services);
        builder.Services.AddResponseCaching();

        var app = builder.Build();
        app.UseResponseCaching();
        MapEndpoints(app);
        return app;
    }

    // After: Output Caching with the Response Caching semantics. Only the two caching lines change.
    public override WebApplication CreateOutputCachingApp(WebApplicationBuilder builder)
    {
        ConfigureServices(builder.Services);
        builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.UseResponseCachingSemantics()));

        var app = builder.Build();
        app.UseOutputCache();
        MapEndpoints(app);
        return app;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddResponseCompression();
    }

    public override void MapEndpoints(WebApplication app)
    {
        app.UseResponseCompression();

        var counter = 0;
        app.MapGet("/", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=10";
            return $"response #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. Accept-Encoding: gzip: endpoint runs", ConfigureRequest: AcceptEncoding("gzip")),
        new("2. Accept-Encoding: gzip: served from cache", ConfigureRequest: AcceptEncoding("gzip")),
        new("3. No Accept-Encoding: endpoint runs, cached for all"),
        new("4. No Accept-Encoding: served from cache"),
        new("5. Accept-Encoding: br: uncompressed from cache", ConfigureRequest: AcceptEncoding("br")),
        new("6. Accept-Encoding: gzip: uncompressed from cache", ConfigureRequest: AcceptEncoding("gzip")),
    ];

    private static Action<System.Net.Http.HttpRequestMessage> AcceptEncoding(string encoding)
        => request => request.Headers.TryAddWithoutValidation("Accept-Encoding", encoding);
}
