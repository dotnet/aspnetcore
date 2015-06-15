// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting.Internal;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        public static void Initialize(this IHostingEnvironment hostingEnvironment, string applicationBasePath, string environmentName)
        {
            hostingEnvironment.WebRootPath = HostingUtilities.GetWebRoot(applicationBasePath);
            if (!Directory.Exists(hostingEnvironment.WebRootPath))
            {
                Directory.CreateDirectory(hostingEnvironment.WebRootPath);
            }
            hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(hostingEnvironment.WebRootPath);
            hostingEnvironment.EnvironmentName = environmentName ?? hostingEnvironment.EnvironmentName;
        }
    }
}