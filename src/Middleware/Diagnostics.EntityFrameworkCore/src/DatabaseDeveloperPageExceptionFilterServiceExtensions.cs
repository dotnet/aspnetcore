// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Service extension methods for the <see cref="DatabaseDeveloperPageExceptionFilter"/>.
/// </summary>
public static class DatabaseDeveloperPageExceptionFilterServiceExtensions
{
    /// <summary>
    /// In combination with UseDeveloperExceptionPage, this captures database-related exceptions that can be resolved by using Entity Framework migrations.
    /// When these exceptions occur, an HTML response with details about possible actions to resolve the issue is generated.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns></returns>
    /// <remarks>
    /// This should only be enabled in the Development environment.
    /// </remarks>
    [RequiresDynamicCode("DbContext migrations operations are not supported with NativeAOT")]
    public static IServiceCollection AddDatabaseDeveloperPageExceptionFilter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(new ServiceDescriptor(typeof(IDeveloperPageExceptionFilter), typeof(DatabaseDeveloperPageExceptionFilter), ServiceLifetime.Singleton));

        return services;
    }
}
