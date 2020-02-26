// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace RedisCacheSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RunSampleAsync().Wait();
        }

        /// <summary>
        /// This sample assumes that a redis server is running on the local machine. You can set this up by doing the following:
        /// Install this chocolatey package: http://chocolatey.org/packages/redis-64/
        /// run "redis-server" from command prompt.
        /// </summary>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task RunSampleAsync()
        {
            var key = "myKey";
            var message = "Hello, World!";
            var value = Encoding.UTF8.GetBytes(message);

            Console.WriteLine("Connecting to cache");
            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = "localhost",
                InstanceName = "SampleInstance"
            });
            Console.WriteLine("Connected");

            Console.WriteLine($"Setting value '{message}' in cache");
            await cache.SetAsync(key, value, new DistributedCacheEntryOptions());
            Console.WriteLine("Set");

            Console.WriteLine("Getting value from cache");
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.WriteLine("Refreshing value in cache");
            await cache.RefreshAsync(key);
            Console.WriteLine("Refreshed");

            Console.WriteLine("Removing value from cache");
            await cache.RemoveAsync(key);
            Console.WriteLine("Removed");

            Console.WriteLine("Getting value from cache again");
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.ReadLine();
        }
    }
}
