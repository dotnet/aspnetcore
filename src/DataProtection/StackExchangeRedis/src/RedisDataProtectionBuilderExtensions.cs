// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Contains Redis-specific extension methods for modifying a <see cref="IDataProtectionBuilder"/>.
/// </summary>
public static class StackExchangeRedisDataProtectionBuilderExtensions
{
    private const string DataProtectionKeysName = "DataProtection-Keys";

    /// <summary>
    /// Configures the data protection system to persist keys to specified key in Redis database
    /// </summary>
    /// <param name="builder">The builder instance to modify.</param>
    /// <param name="databaseFactory">The delegate used to create <see cref="IDatabase"/> instances.</param>
    /// <param name="key">The <see cref="RedisKey"/> used to store key list.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToStackExchangeRedis(this IDataProtectionBuilder builder, Func<IDatabase> databaseFactory, RedisKey key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(databaseFactory);
        return PersistKeysToStackExchangeRedisInternal(builder, _ => databaseFactory(), key);
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the default key ('DataProtection-Keys') in Redis database
    /// </summary>
    /// <param name="builder">The builder instance to modify.</param>
    /// <param name="connectionMultiplexer">The <see cref="IConnectionMultiplexer"/> for database access.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToStackExchangeRedis(this IDataProtectionBuilder builder, IConnectionMultiplexer connectionMultiplexer)
    {
        return PersistKeysToStackExchangeRedis(builder, connectionMultiplexer, DataProtectionKeysName);
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the specified key in Redis database
    /// </summary>
    /// <param name="builder">The builder instance to modify.</param>
    /// <param name="connectionMultiplexer">The <see cref="IConnectionMultiplexer"/> for database access.</param>
    /// <param name="key">The <see cref="RedisKey"/> used to store key list.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToStackExchangeRedis(this IDataProtectionBuilder builder, IConnectionMultiplexer connectionMultiplexer, RedisKey key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(connectionMultiplexer);
        return PersistKeysToStackExchangeRedisInternal(builder, _ => connectionMultiplexer.GetDatabase(), key);
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the default key ('DataProtection-Keys') in Redis database
    /// </summary>
    /// <param name="builder">The builder instance to modify.</param>
    /// <param name="databaseFactory">A factory function that uses <see cref="IServiceProvider"/> to create an <see cref="IDatabase"/> instance.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToStackExchangeRedis(this IDataProtectionBuilder builder, Func<IServiceProvider, IDatabase> databaseFactory)
    {
        return PersistKeysToStackExchangeRedis(builder, databaseFactory, DataProtectionKeysName);
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the specified key in Redis database
    /// </summary>
    /// <param name="builder">The builder instance to modify.</param>
    /// <param name="databaseFactory">A factory function that uses <see cref="IServiceProvider"/> to create an <see cref="IDatabase"/> instance.</param>
    /// <param name="key">The <see cref="RedisKey"/> used to store key list.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToStackExchangeRedis(this IDataProtectionBuilder builder, Func<IServiceProvider, IDatabase> databaseFactory, RedisKey key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(databaseFactory);
        return PersistKeysToStackExchangeRedisInternal(builder, databaseFactory, key);
    }

    private static IDataProtectionBuilder PersistKeysToStackExchangeRedisInternal(IDataProtectionBuilder builder, Func<IServiceProvider, IDatabase> databaseFactory, RedisKey key)
    {
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new RedisXmlRepository(() => databaseFactory(services), key);
            });
        });

        return builder;
    }
}
