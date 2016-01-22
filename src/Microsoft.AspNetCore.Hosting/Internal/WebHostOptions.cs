// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHostOptions
    {
        private const string OldEnvironmentKey = "ENV";

        public WebHostOptions()
        {
        }

        public WebHostOptions(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Application = configuration[WebHostDefaults.ApplicationKey];
            DetailedErrors = ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[WebHostDefaults.EnvironmentKey] ?? configuration[OldEnvironmentKey];
            ServerFactoryLocation = configuration[WebHostDefaults.ServerKey];
            WebRoot = configuration[WebHostDefaults.WebRootKey];
            ApplicationBasePath = configuration[WebHostDefaults.ApplicationBaseKey];
        }

        public string Application { get; set; }

        public bool DetailedErrors { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string ServerFactoryLocation { get; set; }

        public string WebRoot { get; set; }

        public string ApplicationBasePath { get; set; }

        private static bool ParseBool(IConfiguration configuration, string key)
        {
            return string.Equals("true", configuration[key], StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration[key], StringComparison.OrdinalIgnoreCase);
        }
    }
}