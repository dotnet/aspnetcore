// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Redis
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Connect
            var redis = ConnectionMultiplexer.Connect("localhost:6379");

            // Configure
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddDataProtection()
                .PersistKeysToRedis(redis, "DataProtection-Keys");

            var services = serviceCollection.BuildServiceProvider();
            var loggerFactory = services.GetService<LoggerFactory>();
            loggerFactory.AddConsole();

            // Run a sample payload
            var protector = services.GetDataProtector("sample-purpose");
            var protectedData = protector.Protect("Hello world!");
            Console.WriteLine(protectedData);
        }
    }
}
