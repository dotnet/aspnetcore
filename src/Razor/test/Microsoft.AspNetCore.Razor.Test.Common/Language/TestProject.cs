// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestProject
    {
        public static string GetProjectDirectory(string directoryHint)
        {
            var repoRoot = SearchUp(AppContext.BaseDirectory, "global.json");
            if (repoRoot == null)
            {
                repoRoot = AppContext.BaseDirectory;
            }
            
            var projectDirectory = Path.Combine(repoRoot, "src", "Razor", directoryHint, "test");

            if (string.Equals(directoryHint, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
            {
                projectDirectory = Path.Combine(repoRoot, "src", "Razor", "Microsoft.AspNetCore.Razor.Language", "test");
            }

            return projectDirectory;
        }

        public static string GetProjectDirectory(Type type)
        {
            var repoRoot = SearchUp(AppContext.BaseDirectory, "global.json");
            if (repoRoot == null)
            {
                repoRoot = AppContext.BaseDirectory;
            }

            var assemblyName = type.Assembly.GetName().Name;
            var projectDirectory = Path.Combine(repoRoot, "src", "Razor", assemblyName, "test");
            if (string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
            {
                projectDirectory = Path.Combine(repoRoot, "src", "Razor", "Microsoft.AspNetCore.Razor.Language", "test");
            }

            return projectDirectory;
        }

        private static string SearchUp(string baseDirectory, string fileName)
        {
            var directoryInfo = new DirectoryInfo(baseDirectory);
            do
            {
                var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, fileName));
                if (fileInfo.Exists)
                {
                    return fileInfo.DirectoryName;
                }
                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            return null;
        }
    }
}
