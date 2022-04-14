// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Extension methods for registering OpenAPI dependencies in services.
/// </summary>
public static class OpenApiServicesExtensions
{
    /// <summary>
    /// Registers an <see cref="OpenApiGenerator" /> onto the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public static IServiceCollection AddOpenApiGenerator(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<OpenApiGenerator>();
        return services;
    }
}
