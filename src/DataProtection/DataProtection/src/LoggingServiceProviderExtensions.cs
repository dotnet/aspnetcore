// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace System;

/// <summary>
/// Helpful logging-related extension methods on <see cref="IServiceProvider"/>.
/// </summary>
internal static class LoggingServiceProviderExtensions
{
    /// <summary>
    /// Retrieves an instance of <see cref="ILogger"/> given the type name <typeparamref name="T"/>.
    /// This is equivalent to <see cref="LoggerFactoryExtensions.CreateLogger{T}(ILoggerFactory)"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="ILogger"/> instance, or null if <paramref name="services"/> is null or the
    /// <see cref="IServiceProvider"/> cannot produce an <see cref="ILoggerFactory"/>.
    /// </returns>
    public static ILogger GetLogger<T>(this IServiceProvider? services)
    {
        return GetLogger(services, typeof(T));
    }

    /// <summary>
    /// Retrieves an instance of <see cref="ILogger"/> given the type name <paramref name="type"/>.
    /// This is equivalent to <see cref="LoggerFactoryExtensions.CreateLogger{T}(ILoggerFactory)"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="ILogger"/> instance, or null if <paramref name="services"/> is null or the
    /// <see cref="IServiceProvider"/> cannot produce an <see cref="ILoggerFactory"/>.
    /// </returns>
    public static ILogger GetLogger(this IServiceProvider? services, Type type)
    {
        // Compiler won't allow us to use static types as the type parameter
        // for the call to CreateLogger<T>, so we'll duplicate its logic here.
        return services?.GetService<ILoggerFactory>()?.CreateLogger(type.FullName!) ?? NullLogger.Instance;
    }
}
