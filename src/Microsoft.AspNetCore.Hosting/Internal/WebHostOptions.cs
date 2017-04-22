// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHostOptions
    {
        public WebHostOptions() { }

        public WebHostOptions(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ApplicationName = configuration[WebHostDefaults.ApplicationKey];
            StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
            DetailedErrors = ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[WebHostDefaults.EnvironmentKey];
            WebRoot = configuration[WebHostDefaults.WebRootKey];
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
            HostingStartupAssemblies = configuration[WebHostDefaults.HostingStartupAssembliesKey]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            PreferHostingUrls = ParseBool(configuration, WebHostDefaults.PreferHostingUrlsKey);

            var timeout = configuration[WebHostDefaults.ShutdownTimeoutKey];
            if (!string.IsNullOrEmpty(timeout)
                && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
            }
        }

        public string ApplicationName { get; set; }

        public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

        public bool DetailedErrors { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string StartupAssembly { get; set; }

        public string WebRoot { get; set; }

        public string ContentRootPath { get; set; }

        public bool PreferHostingUrls { get; set; }

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        private static bool ParseBool(IConfiguration configuration, string key)
        {
            return string.Equals("true", configuration[key], StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration[key], StringComparison.OrdinalIgnoreCase);
        }
    }
}