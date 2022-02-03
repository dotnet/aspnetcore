// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure JSON serialization behavior.
/// </summary>
public static class RouteHandlerJsonServiceExtensions
{
    /// <summary>
    /// Configures options used for reading and writing JSON by route handlers.
    /// </summary>
    /// <remarks>
    /// The options configured here will only affect JSON returned and processed
    /// from route handlers, not controllers, when using <see cref="O:Microsoft.AspNetCore.Http.HttpRequestJsonExtensions.ReadFromJsonAsync" />
    /// and <see cref="O:Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync" />.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure options on.</param>
    /// <param name="configureOptions">The <see cref="Action{JsonOptions}"/> to configure the
    /// <see cref="JsonOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ConfigureRouteHandlerJsonOptions(this IServiceCollection services, Action<JsonOptions> configureOptions)
    {
        services.Configure<JsonOptions>(configureOptions);
        return services;
    }
}
