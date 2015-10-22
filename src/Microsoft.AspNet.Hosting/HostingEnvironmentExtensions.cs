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
            var webRoot = config?[WebRootKey];
            if (webRoot == null)
            {
                // Default to /wwwroot if it exists.
                var wwwroot = Path.Combine(applicationBasePath, "wwwroot");
                if (Directory.Exists(wwwroot))
                {
                    hostingEnvironment.WebRootPath = wwwroot;
                }
                else
                {
                    hostingEnvironment.WebRootPath = applicationBasePath;
                }
            }
            else
            {
                hostingEnvironment.WebRootPath = Path.Combine(applicationBasePath, webRoot);
            }

            hostingEnvironment.WebRootPath = Path.GetFullPath(hostingEnvironment.WebRootPath);

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