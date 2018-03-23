// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestProject
    {
        public static string GetProjectDirectory(Type type)
        {
            var solutionDir = TestPathUtilities.GetSolutionRootDirectory("Razor");

            var assemblyName = type.Assembly.GetName().Name;
            var projectDirectory = Path.Combine(solutionDir, "test", assemblyName);
            if (!Directory.Exists(projectDirectory))
            {
                throw new InvalidOperationException(
$@"Could not locate project directory for type {type.FullName}.
Directory probe path: {projectDirectory}.");
            }

            return projectDirectory;
        }
    }
}
