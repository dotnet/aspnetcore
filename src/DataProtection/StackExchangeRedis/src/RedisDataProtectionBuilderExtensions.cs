// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;
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
        return PersistKeysToStackExchangeRedisInternal(builder, databaseFactory, key);
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
        return PersistKeysToStackExchangeRedisInternal(builder, () => connectionMultiplexer.GetDatabase(), key);
    }

    private static IDataProtectionBuilder PersistKeysToStackExchangeRedisInternal(IDataProtectionBuilder builder, Func<IDatabase> databaseFactory, RedisKey key)
    {
        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new RedisXmlRepository(databaseFactory, key);
        });
        return builder;
    }
}
