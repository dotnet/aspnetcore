// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace ServerComparison.FunctionalTests
{
    public class Helpers
    {
        public static string GetApplicationPath()
        {
            var applicationBasePath = AppContext.BaseDirectory;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "FunctionalTests.sln"));
                if (solutionFileInfo.Exists)
                {
                    return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "testassets", "TestSites"));
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be found using {applicationBasePath}");
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig, string nginxConfig)
        {
            var applicationBasePath = AppContext.BaseDirectory;

            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(Path.Combine(applicationBasePath, iisConfig));
            }
            else if (serverType == ServerType.Nginx)
            {
                content = File.ReadAllText(Path.Combine(applicationBasePath, nginxConfig));
            }

            return content;
        }

        public static string GetTargetFramework(RuntimeFlavor runtimeFlavor)
        {
            if (runtimeFlavor == RuntimeFlavor.Clr)
            {
                return "net461";
            }
            else if (runtimeFlavor == RuntimeFlavor.CoreClr)
            {
#if NETCOREAPP2_0
                return "netcoreapp2.0";
#elif NETCOREAPP2_1 || NET461
                return "netcoreapp2.1";
#else
#error Target frameworks need to be updated.
#endif
            }

            throw new ArgumentException($"Unknown RuntimeFlavor '{runtimeFlavor}'");
        }
    }
}
