// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace ProfilingSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Runs several concurrent threads that access an item that periodically expires and is re-created.
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            string key = "MyKey";

            var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMilliseconds(50));

            var tasks = new List<Task>();
            for (int threads = 0; threads < 100; threads++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < 110000; i++)
                    {
                        if (!cache.TryGetValue(key, out object value))
                        {
                            // Fake expensive object creation.
                            for (int j = 0; j < 1000000; j++)
                            {
                            }

                            cache.Set(key, new object(), options);
                        }
                    }
                });
                tasks.Add(task);
            }

            Console.WriteLine("Running");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Done");
        }
    }
}
