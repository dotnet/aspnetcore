// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Server.Testing;

namespace ServerComparison.FunctionalTests
{
    public class Helpers
    {
        public static string GetApplicationPath(ApplicationType applicationType)
        {
            return Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", 
                    applicationType == ApplicationType.Standalone? "ServerComparison.TestSites.Standalone" : "ServerComparison.TestSites"));
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig, string nginxConfig)
        {
            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(iisConfig);
            }
            else if (serverType == ServerType.Nginx)
            {
                content = File.ReadAllText(nginxConfig);
            }

            return content;
        }
    }
}