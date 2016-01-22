// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Identity.EntityFramework.Test.Utilities
{
    public static class TestEnvironment
    {
        public static IConfiguration Config { get; }

        static TestEnvironment()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(".")
                .AddJsonFile("config.json", optional: true)
                .AddJsonFile("config.test.json", optional: true)
                .AddEnvironmentVariables();

            Config = configBuilder.Build();
        }
    }
}
