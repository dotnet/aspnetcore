// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the exception handler middleware.
/// </summary>
public static class ExceptionHandlerServiceCollectionExtensions
{
    /// <summary>
    /// Adds services and options for the exception handler middleware.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="ExceptionHandlerOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddExceptionHandler(this IServiceCollection services, Action<ExceptionHandlerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.Configure(configureOptions);
    }

    /// <summary>
    /// Adds services and options for the exception handler middleware.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="ExceptionHandlerOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddExceptionHandler<TService>(this IServiceCollection services, Action<ExceptionHandlerOptions, TService> configureOptions) where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<ExceptionHandlerOptions>().Configure(configureOptions);
        return services;
    }
}
