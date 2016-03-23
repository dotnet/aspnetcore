// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Server.Testing;

namespace ServerComparison.FunctionalTests
{
    public class Helpers
    {
        public static string GetApplicationPath()
        {
            return Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "ServerComparison.TestSites"));
        }

        public static string GetConfigContent(ServerType serverType)
        {
            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText("Http.config");
            }
            else if (serverType == ServerType.Nginx)
            {
                content = File.ReadAllText("nginx.conf");
            }

            return content;
        }
    }
}