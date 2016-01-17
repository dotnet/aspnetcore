// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostConfiguration
    {
        public static IConfiguration GetDefault()
        {
            return GetDefault(args: null);
        }

        public static IConfiguration GetDefault(string[] args)
        {
            var defaultSettings = new Dictionary<string, string>
            {
                { WebHostDefaults.CaptureStartupErrorsKey, "true" }
            };

            // We are adding all environment variables first and then adding the ASPNET_ ones
            // with the prefix removed to unify with the command line and config file formats
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(defaultSettings)
                .AddJsonFile(WebHostDefaults.HostingJsonFile, optional: true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(prefix: WebHostDefaults.EnvironmentVariablesPrefix);

            if (args != null)
            {
                configBuilder.AddCommandLine(args);
            }

            return configBuilder.Build();
        }
    }

}
