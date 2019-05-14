// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;

namespace Microsoft.AspNetCore.DataProtection
{
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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (databaseFactory == null)
            {
                throw new ArgumentNullException(nameof(databaseFactory));
            }
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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }
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
}