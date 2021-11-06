// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace ServerComparison.FunctionalTests;

public class Helpers
{
    public static string GetApplicationPath()
    {
        var applicationBasePath = AppContext.BaseDirectory;

        var directoryInfo = new DirectoryInfo(applicationBasePath);
        do
        {
            var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "FunctionalTests.slnf"));
            if (solutionFileInfo.Exists)
            {
                return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "..", "..", "testassets", "ServerComparison.TestSites"));
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo.Parent != null);

        throw new Exception($"Solution root could not be found using {applicationBasePath}");
    }

    public static string GetNginxConfigContent(string nginxConfig)
    {
        var applicationBasePath = AppContext.BaseDirectory;
        var content = File.ReadAllText(Path.Combine(applicationBasePath, nginxConfig));
        return content;
    }
}
