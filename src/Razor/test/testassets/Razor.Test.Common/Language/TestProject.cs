// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestProject
    {
        private static Dictionary<string, string> _assemblyFolderLookup = new Dictionary<string, string>()
        {
            { "Microsoft.AspNetCore.Razor.Language.Test", "Razor.Language" },
            { "Microsoft.AspNetCore.Mvc.Razor.Extensions.Test", "Mvc.Razor.Extensions" },
            { "Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.Test", "Mvc.Razor.Extensions.Version1_X" },
            { "RazorPageGenerator.Test", "RazorPageGenerator" },
        };

        public static string GetProjectDirectory(Type type)
        {
            var solutionDir = TestPathUtilities.GetSolutionRootDirectory("Razor");

            var assemblyName = type.Assembly.GetName().Name;
            var folderName = assemblyName;
            if (_assemblyFolderLookup.ContainsKey(assemblyName))
            {
                folderName = _assemblyFolderLookup[assemblyName];
            }
            var projectDirectory = Path.Combine(solutionDir, folderName, "test");
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
