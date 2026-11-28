// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace ResponseCachingCompatibility.Scenarios;

/// <summary>
/// Response Caching doesn't cache requests with an Authorization header, but it ignores the authenticated user otherwise:
/// a user authenticated with a cookie is served the cached responses, like anonymous users.
/// </summary>
/// <remarks>
/// The default Output Caching policy doesn't cache any request of an authenticated user.
/// The authentication middleware is simulated: a "user" cookie authenticates the request.
/// </remarks>
public sealed class Authentication : Scenario
{
    public override string Description => "'Authorization' header and cookie-authenticated users";

    // Before: Response Caching.
    public override WebApplication CreateResponseCachingApp(WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCaching();

        var app = builder.Build();
        ConfigurePipeline(app);
        app.UseResponseCaching();
        MapEndpoints(app);
        return app;
    }

    // After: Output Caching with the Response Caching semantics. Only the two caching lines change.
    public override WebApplication CreateOutputCachingApp(WebApplicationBuilder builder)
    {
        builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.UseResponseCachingSemantics()));

        var app = builder.Build();
        ConfigurePipeline(app);
        app.UseOutputCache();
        MapEndpoints(app);
        return app;
    }

    // Stands for UseAuthentication() with cookie authentication, which runs before the caching middleware.
    public override void ConfigurePipeline(WebApplication app)
    {
        app.Use((context, next) =>
        {
            if (context.Request.Cookies["user"] is { } user)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, user)], "Cookies"));
            }

            return next(context);
        });
    }

    public override void MapEndpoints(WebApplication app)
    {
        var counter = 0;
        app.MapGet("/", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=10";
            return $"hello {context.User.Identity?.Name ?? "anonymous"} #{Interlocked.Increment(ref counter)}";
        });
    }

    public override IReadOnlyList<Step> Steps =>
    [
        new("1. Anonymous: endpoint runs"),
        new("2. Anonymous: served from cache"),
        new("3. 'Authorization' header: endpoint runs", ConfigureRequest: Authorization),
        new("4. 'Authorization' header: not cached, endpoint runs", ConfigureRequest: Authorization),
        new("5. Cookie user: served the cached anonymous response", ConfigureRequest: UserCookie),
        new("6. Cookie user: served the cached anonymous response", ConfigureRequest: UserCookie),
    ];

    private static void Authorization(System.Net.Http.HttpRequestMessage request)
        => request.Headers.TryAddWithoutValidation("Authorization", "Bearer token");

    private static void UserCookie(System.Net.Http.HttpRequestMessage request)
        => request.Headers.TryAddWithoutValidation("Cookie", "user=alice");
}
