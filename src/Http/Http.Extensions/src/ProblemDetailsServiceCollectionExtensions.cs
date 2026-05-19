// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection"/>.
/// </summary>
public static class ProblemDetailsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for creation of <see cref="ProblemDetails"/> for failed requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddProblemDetails(configure: null);
    }

    /// <summary>
    /// Adds services required for creation of <see cref="ProblemDetails"/> for failed requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The <see cref="ProblemDetailsOptions"/> to configure the services with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProblemDetails(
        this IServiceCollection services,
        Action<ProblemDetailsOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Adding default services;
        services.TryAddSingleton<IProblemDetailsService, ProblemDetailsService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProblemDetailsWriter, DefaultProblemDetailsWriter>());
        // Use IConfigureOptions (instead of post-configure) so the registration gets added/invoked relative to when AddProblemDetails() is called.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JsonOptions>, ProblemDetailsJsonOptionsSetup>());

        if (configure != null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
