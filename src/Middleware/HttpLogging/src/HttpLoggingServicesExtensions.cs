// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the HttpLogging middleware.
/// </summary>
public static class HttpLoggingServicesExtensions
{
    /// <summary>
    /// Adds HTTP Logging services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.TryAddSingleton(ObjectPool.ObjectPool.Create<HttpLoggingInterceptorContext>());
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }

    /// <summary>
    /// Adds HTTP Logging services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="HttpLoggingOptions"/>.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<HttpLoggingOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddHttpLogging();
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// Registers the given type as a <see cref="IHttpLoggingInterceptor"/> in the DI container.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IHttpLoggingInterceptor"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddHttpLoggingInterceptor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        (this IServiceCollection services) where T : class, IHttpLoggingInterceptor
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpLoggingInterceptor, T>());
        return services;
    }

    /// <summary>
    /// Adds W3C Logging services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="W3CLoggerOptions"/>.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddW3CLogging(this IServiceCollection services, Action<W3CLoggerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddSingleton<W3CLoggerProcessor>();
        services.AddSingleton<W3CLogger>();
        return services;
    }
}
