// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestProject
    {
        public static string GetProjectDirectory(Type type)
        {
            var repoRoot = SearchUp(AppContext.BaseDirectory, "global.json");
            var assemblyName = type.Assembly.GetName().Name;
            var projectDirectory = Path.Combine(repoRoot, "src", "Razor", "test", assemblyName);
            if (!Directory.Exists(projectDirectory))
            {
                throw new InvalidOperationException($@"Could not locate project directory for type {type.FullName}. Directory probe path: {projectDirectory}.");
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

            throw new Exception($"File {fileName} could not be found in {baseDirectory} or its parent directories.");
        }
    }
}
