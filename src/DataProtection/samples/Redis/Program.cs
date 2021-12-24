// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedisSample;

public class Program
{
    public static void Main(string[] args)
    {
        // Connect
        var redis = ConnectionMultiplexer.Connect("localhost:6379");

        // Configure
        using (var services = new ServiceCollection()
            .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
            .Services
            .BuildServiceProvider())
        {
            // Run a sample payload
            var protector = services.GetDataProtector("sample-purpose");
            var protectedData = protector.Protect("Hello world!");
            Console.WriteLine(protectedData);
        }
    }
}
