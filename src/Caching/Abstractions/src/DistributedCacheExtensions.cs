// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed
{
    /// <summary>
    /// Extension methods for setting data in an <see cref="IDistributedCache" />.
    /// </summary>
    public static class DistributedCacheExtensions
    {
        /// <summary>
        /// Sets a sequence of bytes in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static void Set(this IDistributedCache cache, string key, byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            cache.Set(key, value, new DistributedCacheEntryOptions());
        }

        /// <summary>
        /// Asynchronously sets a sequence of bytes in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous set operation.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static Task SetAsync(this IDistributedCache cache, string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return cache.SetAsync(key, value, new DistributedCacheEntryOptions(), token);
        }

        /// <summary>
        /// Sets a string in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static void SetString(this IDistributedCache cache, string key, string value)
        {
            cache.SetString(key, value, new DistributedCacheEntryOptions());
        }

        /// <summary>
        /// Sets a string in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <param name="options">The cache options for the entry.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static void SetString(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            cache.Set(key, Encoding.UTF8.GetBytes(value), options);
        }

        /// <summary>
        /// Asynchronously sets a string in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous set operation.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static Task SetStringAsync(this IDistributedCache cache, string key, string value, CancellationToken token = default(CancellationToken))
        {
            return cache.SetStringAsync(key, value, new DistributedCacheEntryOptions(), token);
        }

        /// <summary>
        /// Asynchronously sets a string in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <param name="options">The cache options for the entry.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous set operation.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        public static Task SetStringAsync(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options, token);
        }

        /// <summary>
        /// Gets a string from the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to get the stored data for.</param>
        /// <returns>The string value from the stored cache key.</returns>
        public static string GetString(this IDistributedCache cache, string key)
        {
            var data = cache.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        /// <summary>
        /// Asynchronously gets a string from the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to get the stored data for.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
        /// <returns>A task that gets the string value from the stored cache key.</returns>
        public static async Task<string> GetStringAsync(this IDistributedCache cache, string key, CancellationToken token = default(CancellationToken))
        {
            var data = await cache.GetAsync(key, token);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }
    }
}