// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace ResponseCachingCompatibility;

/// <summary>
/// A Response Caching usage, and the same application migrated to Output Caching.
/// Add a new scenario by deriving from this class; it's discovered automatically.
/// </summary>
public abstract class Scenario
{
    public string Name => GetType().Name;

    public abstract string Description { get; }

    /// <summary>
    /// Before: the application using Response Caching.
    /// </summary>
    public abstract WebApplication CreateResponseCachingApp(WebApplicationBuilder builder);

    /// <summary>
    /// After: the same application migrated to Output Caching.
    /// </summary>
    public abstract WebApplication CreateOutputCachingApp(WebApplicationBuilder builder);

    /// <summary>
    /// For comparison: the same application with the default Output Caching policy, which ignores the Cache-Control headers.
    /// </summary>
    public virtual WebApplication CreateDefaultOutputCachingApp(WebApplicationBuilder builder)
    {
        ConfigureServices(builder.Services);
        builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.Cache()));

        var app = builder.Build();
        ConfigurePipeline(app);
        app.UseOutputCache();
        MapEndpoints(app);
        return app;
    }

    /// <summary>
    /// The services needed by the endpoints, identical in all applications.
    /// </summary>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// The middleware running before the caching middleware, e.g. authentication, identical in all applications.
    /// </summary>
    public virtual void ConfigurePipeline(WebApplication app)
    {
    }

    /// <summary>
    /// The endpoints, and the middleware running after the caching middleware, identical in all applications.
    /// </summary>
    public abstract void MapEndpoints(WebApplication app);

    /// <summary>
    /// The requests sent to every application when comparing them.
    /// </summary>
    public abstract IReadOnlyList<Step> Steps { get; }

    /// <summary>
    /// The applications compared by the scenario. Override to add variations.
    /// </summary>
    public virtual IReadOnlyList<ScenarioApplication> Applications =>
    [
        new("responsecaching", "ResponseCaching", CreateResponseCachingApp),
        new("outputcaching", "OutputCaching + policy", CreateOutputCachingApp),
        new("default", "OutputCaching default", CreateDefaultOutputCachingApp),
    ];
}

/// <summary>
/// An application compared by a scenario.
/// </summary>
/// <param name="Key">The name used to select the application from the command line.</param>
/// <param name="Name">The column header in the comparison.</param>
/// <param name="Create">Creates the application.</param>
public sealed record ScenarioApplication(string Key, string Name, Func<WebApplicationBuilder, WebApplication> Create);

/// <summary>
/// A request sent to the applications when comparing them.
/// </summary>
/// <param name="Description">What the step demonstrates.</param>
/// <param name="Path">The request path and query.</param>
/// <param name="ConfigureRequest">Sets request headers.</param>
/// <param name="DelaySeconds">The time to wait before sending the request.</param>
/// <param name="Parallel">The number of identical requests sent concurrently.</param>
public sealed record Step(string Description, string Path = "/", Action<HttpRequestMessage>? ConfigureRequest = null, int DelaySeconds = 0, int Parallel = 1);
