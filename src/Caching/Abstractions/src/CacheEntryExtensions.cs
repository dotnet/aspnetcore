// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class CacheEntryExtensions
    {
        /// <summary>
        /// Sets the priority for keeping the cache entry in the cache during a memory pressure tokened cleanup.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="priority"></param>
        public static ICacheEntry SetPriority(
            this ICacheEntry entry,
            CacheItemPriority priority)
        {
            entry.Priority = priority;
            return entry;
        }

        /// <summary>
        /// Expire the cache entry if the given <see cref="IChangeToken"/> expires.
        /// </summary>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        /// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
        public static ICacheEntry AddExpirationToken(
            this ICacheEntry entry,
            IChangeToken expirationToken)
        {
            if (expirationToken == null)
            {
                throw new ArgumentNullException(nameof(expirationToken));
            }

            entry.ExpirationTokens.Add(expirationToken);
            return entry;
        }

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="relative"></param>
        public static ICacheEntry SetAbsoluteExpiration(
            this ICacheEntry entry,
            TimeSpan relative)
        {
            entry.AbsoluteExpirationRelativeToNow = relative;
            return entry;
        }

        /// <summary>
        /// Sets an absolute expiration date for the cache entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="absolute"></param>
        public static ICacheEntry SetAbsoluteExpiration(
            this ICacheEntry entry,
            DateTimeOffset absolute)
        {
            entry.AbsoluteExpiration = absolute;
            return entry;
        }

        /// <summary>
        /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="offset"></param>
        public static ICacheEntry SetSlidingExpiration(
            this ICacheEntry entry,
            TimeSpan offset)
        {
            entry.SlidingExpiration = offset;
            return entry;
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback"></param>
        public static ICacheEntry RegisterPostEvictionCallback(
            this ICacheEntry entry,
            PostEvictionDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return entry.RegisterPostEvictionCallback(callback, state: null);
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public static ICacheEntry RegisterPostEvictionCallback(
            this ICacheEntry entry,
            PostEvictionDelegate callback,
            object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = callback,
                State = state
            });
            return entry;
        }

        /// <summary>
        /// Sets the value of the cache entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="value"></param>
        public static ICacheEntry SetValue(
            this ICacheEntry entry,
            object value)
        {
            entry.Value = value;
            return entry;
        }

        /// <summary>
        /// Sets the size of the cache entry value.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="size"></param>
        public static ICacheEntry SetSize(
            this ICacheEntry entry,
            long size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, $"{nameof(size)} must be non-negative.");
            }

            entry.Size = size;
            return entry;
        }

        /// <summary>
        /// Applies the values of an existing <see cref="MemoryCacheEntryOptions"/> to the entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="options"></param>
        public static ICacheEntry SetOptions(this ICacheEntry entry, MemoryCacheEntryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            entry.AbsoluteExpiration = options.AbsoluteExpiration;
            entry.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            entry.SlidingExpiration = options.SlidingExpiration;
            entry.Priority = options.Priority;
            entry.Size = options.Size;

            foreach (var expirationToken in options.ExpirationTokens)
            {
                entry.AddExpirationToken(expirationToken);
            }

            foreach (var postEvictionCallback in options.PostEvictionCallbacks)
            {
                entry.RegisterPostEvictionCallback(postEvictionCallback.EvictionCallback, postEvictionCallback.State);
            }

            return entry;
        }
    }
}