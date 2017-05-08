// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ApplicationInsights.HostingStartup
{
    internal class ApplicationInsightsLoggerConfiguration
    {
        private const string ApplicationInsightsLoggerFactory = "Microsoft.ApplicationInsights.AspNetCore.Logging.ApplicationInsightsLoggerProvider";
        private const string ApplicationInsightsLoggerLevelSection = "Logging:" + ApplicationInsightsLoggerFactory + ":LogLevel";
        private const string ApplicationInsightsSettingsFile = "ApplicationInsights.settings.json";

        private static readonly KeyValuePair<string, LogLevel>[] _defaultLoggingLevels = {
            new KeyValuePair<string, LogLevel>("Microsoft", LogLevel.Warning),
            new KeyValuePair<string, LogLevel>("System", LogLevel.Warning),
            new KeyValuePair<string, LogLevel>(null, LogLevel.Information)
        };

        public static void ConfigureLogging(IConfigurationBuilder configurationBuilder)
        {
            // Skip adding default rules when debugger is attached
            // we want to send all events to VS
            if (!Debugger.IsAttached)
            {
                configurationBuilder.AddInMemoryCollection(GetDefaultLoggingSettings());
            }

            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                var settingsFile = Path.Combine(home, "site", "diagnostics", ApplicationInsightsSettingsFile);
                configurationBuilder.AddJsonFile(settingsFile, optional: true);
            }
        }

        public static bool ApplyDefaultFilter(string name, LogLevel level)
        {
            foreach (var pair in _defaultLoggingLevels)
            {
                // Default is null
                if (pair.Key == null || name.StartsWith(pair.Key, StringComparison.Ordinal))
                {
                    return level >= pair.Value;
                }
            }

            return false;
        }

        public static bool HasLoggingConfigured(IConfiguration configuration)
        {
            return configuration?.GetSection(ApplicationInsightsLoggerLevelSection) != null;
        }

        private static KeyValuePair<string, string>[] GetDefaultLoggingSettings()
        {
            return _defaultLoggingLevels.Select(pair =>
            {
                var key = pair.Key ?? "Default";
                var optionsKey = $"{ApplicationInsightsLoggerLevelSection}:{key}";
                return new KeyValuePair<string, string>(optionsKey, pair.Value.ToString());
            }).ToArray();
        }
    }
}