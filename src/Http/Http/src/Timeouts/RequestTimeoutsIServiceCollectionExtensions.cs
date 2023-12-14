// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the request timeouts middleware.
/// </summary>
public static class RequestTimeoutsIServiceCollectionExtensions
{
    /// <summary>
    /// Add request timeout services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns></returns>
    public static IServiceCollection AddRequestTimeouts(this IServiceCollection services)
    {
        services.TryAddSingleton<ICancellationTokenLinker, CancellationTokenLinker>();
        return services;
    }

    /// <summary>
    /// Add request timeout services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configure">A delegate to configure the <see cref="RequestTimeoutOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddRequestTimeouts(this IServiceCollection services, Action<RequestTimeoutOptions> configure)
    {
        return services.AddRequestTimeouts().Configure(configure);
    }
}
