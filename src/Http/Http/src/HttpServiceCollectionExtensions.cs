// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring HttpContext services.
/// </summary>
public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Adds a default implementation for the <see cref="IHttpContextAccessor"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHttpContextAccessor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services;
    }
}
