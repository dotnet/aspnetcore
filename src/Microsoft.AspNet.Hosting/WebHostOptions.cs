// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostOptions
    {
        private const string ApplicationKey = "app";
        private const string DetailedErrorsKey = "detailedErrors";
        private const string EnvironmentKey = "environment";
        private const string ServerKey = "server";
        private const string WebRootKey = "webroot";
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

            Application = configuration[ApplicationKey];
            DetailedErrors = string.Equals("true", configuration[DetailedErrorsKey], StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration[DetailedErrorsKey], StringComparison.OrdinalIgnoreCase);
            Environment = configuration[EnvironmentKey] ?? configuration[OldEnvironmentKey];
            Server = configuration[ServerKey];
            WebRoot = configuration[WebRootKey];
        }

        public string Application { get; set; }

        public bool DetailedErrors { get; set; }

        public string Environment { get; set; }

        public string Server { get; set; }

        public string WebRoot { get; set; }
    }
}