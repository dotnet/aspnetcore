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
            var solutionDir = GetSolutionRootDirectory("Blazor");

            var assemblyName = type.Assembly.GetName().Name;

            // Temporary special case during code migration
            // TODO: Remove this when all renames are finished
            var componentsPrefix = "Microsoft.AspNetCore.Blazor.";
            if (assemblyName.StartsWith(componentsPrefix))
            {
                assemblyName = "Microsoft.AspNetCore.Components." + assemblyName.Substring(componentsPrefix.Length);
            }

            var projectDirectory = Path.Combine(solutionDir, "test", assemblyName);
            if (!Directory.Exists(projectDirectory))
            {
                throw new InvalidOperationException(
$@"Could not locate project directory for type {type.FullName}.
Directory probe path: {projectDirectory}.");
            }

            return projectDirectory;
        }

        public static string GetSolutionRootDirectory(string solution)
        {
            var applicationBasePath = AppContext.BaseDirectory;
            var directoryInfo = new DirectoryInfo(applicationBasePath);

            do
            {
                var projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{solution}.sln"));
                if (projectFileInfo.Exists)
                {
                    return projectFileInfo.DirectoryName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution file {solution}.sln could not be found in {applicationBasePath} or its parent directories.");
        }
    }
}
