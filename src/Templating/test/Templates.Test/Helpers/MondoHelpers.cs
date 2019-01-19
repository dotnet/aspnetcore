// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Templates.Test.Helpers
{
    public static class MondoHelpers
    {
        public static string[] GetNupkgFiles()
        {
            var mondoRoot = GetMondoRepoRoot();
#if DEBUG
            var configuration = "Debug";
#else
            var configuration = "Release";
#endif

            return Directory.GetFiles(Path.Combine(mondoRoot, "artifacts", configuration, "packages"), "*.nupkg", SearchOption.AllDirectories);
        }

        private static string GetMondoRepoRoot()
        {
            return FindAncestorDirectoryContaining(".gitmodules");
        }

        private static string FindAncestorDirectoryContaining(string filename)
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, filename)))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new InvalidOperationException($"Could not find any ancestor directory containing {filename} at or above {AppContext.BaseDirectory}");
        }
    }
}
