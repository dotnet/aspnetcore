// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.Redis
{
    internal static class RedisExtensions
    {
        private const string HmGetScript = (@"return redis.call('HMGET', KEYS[1], unpack(ARGV))");

        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members)
        {
            var result = cache.ScriptEvaluate(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
        }

        internal static async Task<RedisValue[]> HashMemberGetAsync(
            this IDatabase cache,
            string key,
            params string[] members)
        {
            var result = await cache.ScriptEvaluateAsync(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
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