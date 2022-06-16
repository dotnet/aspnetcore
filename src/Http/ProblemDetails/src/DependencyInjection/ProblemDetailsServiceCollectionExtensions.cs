// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Adding default services;
        services.TryAddSingleton<ProblemDetailsWriterProvider>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProblemDetailsWriter, DefaultProblemDetailsWriter>());

        return services;
    }

    /// <summary>
    /// Adds services required for creation of <see cref="ProblemDetails"/> for failed requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The routing options to configure the middleware with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProblemDetails(
        this IServiceCollection services,
        Action<ProblemDetailsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Adding default services
        services.AddProblemDetails();

        services.Configure(configureOptions);

        return services;
    }
}
