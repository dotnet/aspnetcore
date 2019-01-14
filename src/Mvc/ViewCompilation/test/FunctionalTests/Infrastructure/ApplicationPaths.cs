// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace FunctionalTests
{
    public static class ApplicationPaths
    {
        private const string SolutionName = "Mvc.sln";

        public static string SolutionDirectory { get; } = GetSolutionDirectory();

        public static string GetTestAppDirectory(string appName) =>
            Path.Combine(SolutionDirectory, "ViewCompilation", "testassets", appName);

        private static string GetSolutionDirectory()
        {
            var applicationBasePath = AppContext.BaseDirectory;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, SolutionName));
                if (solutionFileInfo.Exists)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            } while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution directory could not be found for {applicationBasePath}.");
        }
    }
}
