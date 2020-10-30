// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Mvc
{
    public static class MvcTestSource
    {
        private static readonly string ProjectDirectory = GetProjectDirectory();

        public static TestSource Read(string testClassName, string testMethod)
        {
            var filePath = Path.Combine(ProjectDirectory, "TestFiles", testClassName, testMethod + ".cs");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"TestFile {testMethod} could not be found at {filePath}.", filePath);
            }

            var fileContent = File.ReadAllText(filePath);
            return TestSource.Read(fileContent);
        }

        private static string GetProjectDirectory()
        {
            // Test files are copied to both the bin/ and publish/ folders. Use BaseDirectory on or off Helix.
            return AppContext.BaseDirectory;
        }
    }
}
