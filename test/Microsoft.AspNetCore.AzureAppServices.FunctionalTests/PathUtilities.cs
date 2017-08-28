// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class PathUtilities
    {
        public static string[] GetStoreModules(string dotnetPath)
        {
            var dotnetHome = Path.GetDirectoryName(dotnetPath);
            return new DirectoryInfo(Path.Combine(dotnetHome, "store", "x64", "netcoreapp2.0"))
                .GetDirectories()
                .Select(d => d.Name)
                .ToArray();
        }

        public static string[] GetSharedRuntimeAssemblies(string dotnetPath)
        {
            var dotnetHome = Path.GetDirectoryName(dotnetPath);
            return new DirectoryInfo(Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App"))
                .GetDirectories()
                .Single()
                .GetFiles("*.dll")
                .Select(f => f.Name)
                .ToArray();
        }

        public static string GetBundledAspNetCoreVersion(string dotnetPath)
        {
            var dotnetHome = Path.GetDirectoryName(dotnetPath);
            return new DirectoryInfo(Path.Combine(dotnetHome, "store", "x64", "netcoreapp2.0", "microsoft.aspnetcore"))
                .GetDirectories()
                .Single()
                .Name;
        }
    }
}