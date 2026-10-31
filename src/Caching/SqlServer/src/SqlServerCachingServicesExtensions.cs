// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.SqlServer;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Microsoft SQL Server distributed cache services in an <see cref="IServiceCollection" />.
/// </summary>
public static class SqlServerCachingServicesExtensions
{
    /// <summary>
    /// Adds Microsoft SQL Server distributed caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{SqlServerCacheOptions}"/> to configure the provided <see cref="SqlServerCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDistributedSqlServerCache(this IServiceCollection services, Action<SqlServerCacheOptions> setupAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(setupAction);

        services.AddOptions();
        AddSqlServerCacheServices(services);
        services.Configure(setupAction);

        return services;
    }

    // to enable unit testing
    internal static void AddSqlServerCacheServices(IServiceCollection services)
    {
        services.Add(ServiceDescriptor.Singleton<IDistributedCache, SqlServerCache>());
    }
}
