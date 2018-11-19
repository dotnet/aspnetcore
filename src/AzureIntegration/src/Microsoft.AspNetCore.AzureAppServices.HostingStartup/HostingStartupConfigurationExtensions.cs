// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting
{
    internal static class HostingStartupConfigurationExtensions
    {
        public static IConfiguration GetBaseConfiguration()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();
        }
        public static bool IsEnabled(this IConfiguration configuration, string hostingStartupName, string featureName)
        {
            if (configuration.TryGetOption(hostingStartupName, featureName, out var value))
            {
                value = value.ToLowerInvariant();
                return value != "false" && value != "0";
            }

            return true;
        }

        public static bool TryGetOption(this IConfiguration configuration, string hostingStartupName, string featureName, out string value)
        {
            value = configuration[$"HostingStartup:{hostingStartupName}:{featureName}"];
            return !string.IsNullOrEmpty(value);
        }
    }
}