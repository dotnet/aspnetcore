// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace MemoryCacheFileWatchSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var greeting = "";
            var cacheKey = "cache_key";
            var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "WatchedFiles"));

            do
            {
                if (!cache.TryGetValue(cacheKey, out greeting))
                {
                    using (var streamReader = new StreamReader(fileProvider.GetFileInfo("example.txt").CreateReadStream()))
                    {
                        greeting = streamReader.ReadToEnd();
                        cache.Set(cacheKey, greeting, new MemoryCacheEntryOptions()
                             //Telling the cache to depend on the IChangeToken from watching examples.txt
                             .AddExpirationToken(fileProvider.Watch("example.txt"))
                             .RegisterPostEvictionCallback(
                             (echoKey, value, reason, substate) =>
                             {
                                 Console.WriteLine($"{echoKey} : {value} was evicted due to {reason}");
                             }));
                        Console.WriteLine($"{cacheKey} updated from source.");
                    }
                }
                else
                {
                    Console.WriteLine($"{cacheKey} retrieved from cache.");
                }

                Console.WriteLine(greeting);
                Console.WriteLine("Press any key to continue. Press the ESC key to exit");
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
