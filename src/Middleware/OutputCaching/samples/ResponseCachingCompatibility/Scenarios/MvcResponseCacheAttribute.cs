// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// MVC actions using [ResponseCache], the most common way to use Response Caching. The attribute only writes the
/// Cache-Control response header, so the controllers don't change when migrating.
/// </summary>
public sealed class MvcResponseCacheAttribute : Scenario
{
    public override string Description => "MVC actions with [ResponseCache(Duration)], Location = Client, and NoStore";

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
        services.AddMvcCore();
        services.AddSingleton<EndpointCounter>();
    }

    public override void MapEndpoints(WebApplication app) => app.MapControllers();

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. [ResponseCache(Duration = 3)]: endpoint runs", "/mvc/duration"),
        new("2. Same request: served from cache", "/mvc/duration"),
        new("3. Location = Client ('private'): endpoint runs", "/mvc/client"),
        new("4. Same request: not cached on the server", "/mvc/client"),
        new("5. NoStore = true ('no-store'): endpoint runs", "/mvc/nostore"),
        new("6. Same request: not cached", "/mvc/nostore"),
        new("7. Wait 4s, Duration = 3 expired: endpoint runs", "/mvc/duration", DelaySeconds: 4),
    ];
}

[Route("mvc")]
public sealed class ResponseCacheController(EndpointCounter counter) : ControllerBase
{
    // Writes "Cache-Control: public,max-age=3".
    [HttpGet("duration")]
    [ResponseCache(Duration = 3)]
    public string Duration() => $"duration #{counter.Next("duration")}";

    // Writes "Cache-Control: private,max-age=3": only the client may cache the response.
    [HttpGet("client")]
    [ResponseCache(Duration = 3, Location = ResponseCacheLocation.Client)]
    public string Client() => $"client #{counter.Next("client")}";

    // Writes "Cache-Control: no-store,no-cache".
    [HttpGet("nostore")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public string NoStore() => $"nostore #{counter.Next("nostore")}";
}
