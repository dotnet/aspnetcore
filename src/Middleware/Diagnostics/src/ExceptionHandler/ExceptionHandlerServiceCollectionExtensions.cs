// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;

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

    /// <summary>
    /// Adds an `IExceptionHandler` implementation to services. `IExceptionHandler` implementations are used by the exception handler middleware to handle unexpected request exceptions.
    /// Multiple handlers can be added and they're called by the middleware in the order they're added.
    /// </summary>
    /// <typeparam name="T">The type of the exception handler implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddExceptionHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IExceptionHandler
    {
        return services.AddSingleton<IExceptionHandler, T>();
    }
}
