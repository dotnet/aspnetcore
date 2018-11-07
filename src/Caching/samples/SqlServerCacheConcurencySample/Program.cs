// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace SqlServerCacheConcurrencySample
{
    /// <summary>
    /// This sample requires setting up a Microsoft SQL Server based cache database.
    /// 1. Install the .NET Core sql-cache tool globally by installing the dotnet-sql-cache package.
    /// 2. Create a new database in the SQL Server or use an existing one.
    /// 3. Run the command "dotnet sql-cache create <connectionstring> <schemaName> <tableName>" to setup the table and indexes.
    /// 4. Run this sample by doing "dotnet run"
    /// </summary>
    public class Program
    {
        private const string Key = "MyKey";
        private static readonly Random Random = new Random();
        private static DistributedCacheEntryOptions _cacheEntryOptions;

        public static void Main()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            _cacheEntryOptions = new DistributedCacheEntryOptions();
            _cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(10));

            var cache = new SqlServerCache(new SqlServerCacheOptions()
            {
                ConnectionString = configuration["ConnectionString"],
                SchemaName = configuration["SchemaName"],
                TableName = configuration["TableName"]
            });

            SetKey(cache, "0");

            PeriodicallyReadKey(cache, TimeSpan.FromSeconds(1));

            PeriodciallyRemoveKey(cache, TimeSpan.FromSeconds(11));

            PeriodciallySetKey(cache, TimeSpan.FromSeconds(13));

            Console.ReadLine();
            Console.WriteLine("Shutting down");
        }

        private static void SetKey(IDistributedCache cache, string value)
        {
            Console.WriteLine("Setting: " + value);
            cache.Set(Key, Encoding.UTF8.GetBytes(value), _cacheEntryOptions);
        }

        private static void PeriodciallySetKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    SetKey(cache, "A");
                }
            });
        }

        private static void PeriodicallyReadKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    if (Random.Next(3) == 0) // 1/3 chance
                    {
                        // Allow values to expire due to sliding refresh.
                        Console.WriteLine("Read skipped, random choice.");
                    }
                    else
                    {
                        Console.Write("Reading...");
                        object result = cache.Get(Key);
                        if (result != null)
                        {
                            cache.Set(Key, Encoding.UTF8.GetBytes("B"), _cacheEntryOptions);
                        }
                        Console.WriteLine("Read: " + (result ?? "(null)"));
                    }
                }
            });
        }

        private static void PeriodciallyRemoveKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    Console.WriteLine("Removing...");
                    cache.Remove(Key);
                }
            });
        }
    }
}
