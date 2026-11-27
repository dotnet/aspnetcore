// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// [ResponseCache(VaryByQueryKeys = ...)] caches a response per value of the listed query keys. MVC stores the keys in
/// IResponseCachingFeature, which the Response Caching middleware and UseResponseCachingSemantics() provide.
/// </summary>
/// <remarks>
/// The default Output Caching policy doesn't provide the feature, so MVC throws and the request fails with a 500.
/// </remarks>
public sealed class VaryByQueryKeys : Scenario
{
    public override string Description => "MVC action with [ResponseCache(VaryByQueryKeys = [\"page\"])]";

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
        new("1. ?page=1: endpoint runs", "/products?page=1"),
        new("2. ?page=1: served from cache", "/products?page=1"),
        new("3. ?page=2: endpoint runs", "/products?page=2"),
        new("4. ?page=2: served from cache", "/products?page=2"),
        new("5. ?page=1&sort=asc: 'sort' isn't a vary key", "/products?page=1&sort=asc"),
    ];
}

[Route("products")]
public sealed class ProductsController(EndpointCounter counter) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 10, VaryByQueryKeys = ["page"])]
    public string Get(int page) => $"page {page} #{counter.Next("products")}";
}
