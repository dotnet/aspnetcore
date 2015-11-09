// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        public static void Initialize(this IHostingEnvironment hostingEnvironment, string applicationBasePath, WebHostOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var webRoot = options.WebRoot;
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

            var environmentName = options.Environment;
            hostingEnvironment.EnvironmentName = environmentName ?? hostingEnvironment.EnvironmentName;
        }
    }
}