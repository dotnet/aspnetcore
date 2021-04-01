// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis
{
    internal static class RedisExtensions
    {
        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members)
        {
            // TODO: Error checking?
            return cache.HashGet(key, GetRedisMembers(members));
        }

        internal static async Task<RedisValue[]> HashMemberGetAsync(
            this IDatabase cache,
            string key,
            params string[] members)
        {
            // TODO: Error checking?
            return await cache.HashGetAsync(key, GetRedisMembers(members)).ConfigureAwait(false);
        }

        private static RedisValue[] GetRedisMembers(params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }

            return redisMembers;
        }
    }
}
