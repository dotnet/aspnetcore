// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test.Utilities;

public static class TestEnvironment
{
    public static IConfiguration Config { get; }

    static TestEnvironment()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: true)
            .AddJsonFile("config.test.json", optional: true)
            .AddEnvironmentVariables();

        Config = configBuilder.Build();
    }
}
