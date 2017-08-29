// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class PathUtilities
    {
        public static StoreModuleInfo[] GetStoreModules(string dotnetPath)
        {
            var dotnetHome = Path.GetDirectoryName(dotnetPath);
            return new DirectoryInfo(Path.Combine(dotnetHome, "store", "x64", "netcoreapp2.0"))
                .GetDirectories()
                .Select(d => new StoreModuleInfo
                {
                    Name = d.Name,
                    Versions = d.GetDirectories().Select(GetName).ToArray()
                })
                .ToArray();
        }

        public static string[] GetSharedRuntimeAssemblies(string dotnetPath, out string runtimeVersion)
        {
            var dotnetHome = Path.GetDirectoryName(dotnetPath);
            var runtimeDirectory = new DirectoryInfo(Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App"))
                .GetDirectories()
                .Single();

            runtimeVersion = runtimeDirectory.Name;

            return runtimeDirectory
                .GetFiles("*.dll")
                .Select(GetName)
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

        private static string GetName(FileSystemInfo info) => info.Name;

        public class StoreModuleInfo
        {
            public string Name { get; set; }
            public string[] Versions { get; set; }
        }
    }
}