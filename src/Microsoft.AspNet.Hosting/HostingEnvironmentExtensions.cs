// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        private const string OldEnvironmentKey = "ASPNET_ENV";
        private const string EnvironmentKey = "Hosting:Environment";

        private const string WebRootKey = "Hosting:WebRoot";

        public static void Initialize(this IHostingEnvironment hostingEnvironment, string applicationBasePath, IConfiguration config)
        {
            var webRoot = config?[WebRootKey] ?? string.Empty;
            hostingEnvironment.WebRootPath = Path.Combine(applicationBasePath, webRoot);
            if (!Directory.Exists(hostingEnvironment.WebRootPath))
            {
                Directory.CreateDirectory(hostingEnvironment.WebRootPath);
            }
            hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(hostingEnvironment.WebRootPath);

            var environmentName = config?[EnvironmentKey] ?? config?[OldEnvironmentKey];
            hostingEnvironment.EnvironmentName = environmentName ?? hostingEnvironment.EnvironmentName;
        }
    }
}