// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.AspNetCore.DataProtection
{
    internal class TestRedisServer
    {
        public const string ConnectionStringKeyName = "Test:Redis:Server";
        private static readonly IConfigurationRoot _config;

        static TestRedisServer()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("testconfig.json")
                .AddEnvironmentVariables()
                .Build();
        }

        internal static string GetConnectionString()
        {
            return _config[ConnectionStringKeyName];
        }
    }
}