// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.ApplicationInsights.HostingStartup.ApplicationInsightsHostingStartup))]

// To be able to build as <OutputType>Exe</OutputType>
internal class Program { public static void Main() { } }

namespace Microsoft.AspNetCore.ApplicationInsights.HostingStartup
{
    /// <summary>
    /// A dynamic Application Insights lightup experience
    /// </summary>
    public class ApplicationInsightsHostingStartup : IHostingStartup
    {
        private const string ApplicationInsightsLoggerFactory = "Microsoft.ApplicationInsights.AspNetCore.Logging.ApplicationInsightsLoggerProvider";
        private const string ApplicationInsightsSettingsFile = "ApplicationInsights.settings.json";

        private static readonly KeyValuePair<string, string>[] _defaultLoggingLevels = {
            new KeyValuePair<string, string>("Microsoft", "Warning"),
            new KeyValuePair<string, string>("System", "Warning"),
            new KeyValuePair<string, string>("Default", "Information")
        };

        /// <summary>
        /// Calls UseApplicationInsights
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configurationBuilder) => ConfigureLogging(configurationBuilder));
            builder.UseApplicationInsights();

            builder.ConfigureServices(InitializeServices);
        }

        private static void ConfigureLogging(IConfigurationBuilder configurationBuilder)
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

        private static KeyValuePair<string, string>[] GetDefaultLoggingSettings()
        {
            return _defaultLoggingLevels.Select(pair =>
                {
                    var key = $"Logging:{ApplicationInsightsLoggerFactory}:LogLevel:{pair.Key}";
                    return new KeyValuePair<string, string>(key, pair.Value);
                }).ToArray();
        }

        /// <summary>
        /// Adds the Javascript <see cref="TagHelperComponent"/> to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> associated with the application.</param>
        private void InitializeServices(IServiceCollection services)
        {
            services.AddSingleton<IStartupFilter, ApplicationInsightsLoggerStartupFilter>();
            services.AddSingleton<ITagHelperComponent, JavaScriptSnippetTagHelperComponent>();
        }
    }
}
