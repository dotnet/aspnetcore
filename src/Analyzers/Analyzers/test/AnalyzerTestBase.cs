// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    public abstract class AnalyzerTestBase
    {
        private static readonly string ProjectDirectory = GetProjectDirectory();

        public TestSource Read(string source)
        {
            if (!source.EndsWith(".cs"))
            {
                source = source + ".cs";
            }

            var filePath = Path.Combine(ProjectDirectory, "TestFiles", GetType().Name, source);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"TestFile {source} could not be found at {filePath}.", filePath);
            }

            var fileContent = File.ReadAllText(filePath);
            return TestSource.Read(fileContent);
        }

        public Project CreateProject(string source)
        {
            if (!source.EndsWith(".cs"))
            {
                source = source + ".cs";
            }

            var read = Read(source);
            return AnalyzersDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { read.Source, });
        }

        public Task<Compilation> CreateCompilationAsync(string source)
        {
            return CreateProject(source).GetCompilationAsync();
        }

        private static string GetProjectDirectory()
        {
            // On helix we use the published test files
            if (SkipOnHelixAttribute.OnHelix())
            {
                return AppContext.BaseDirectory;
            }

// This test code needs to be updated to support distributed testing.
// See https://github.com/dotnet/aspnetcore/issues/10422
#pragma warning disable 0618
            var solutionDirectory = TestPathUtilities.GetSolutionRootDirectory("Analyzers");
#pragma warning restore 0618
            var projectDirectory = Path.Combine(solutionDirectory, "Analyzers", "test");
            return projectDirectory;
        }
    }
}
