// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.DataProtection;

internal class TestRedisServer
{
    public const string ConnectionStringKeyName = "Test:Redis:Server";
    private static readonly IConfigurationRoot s_config;

    static TestRedisServer()
    {
        s_config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("testconfig.json")
            .AddEnvironmentVariables()
            .Build();
    }

    internal static string GetConnectionString()
    {
        return s_config[ConnectionStringKeyName];
    }
}
