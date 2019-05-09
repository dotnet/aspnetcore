// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    public abstract class AnalyzerTestBase
    {
        private static readonly string ProjectDirectory = GetProjectDirectory();

        public TestSource Read(string source)
        {
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
            return DiagnosticProject.Create(GetType().Assembly, new[] { read.Source, });
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

            var solutionDirectory = TestPathUtilities.GetSolutionRootDirectory("Analyzers");
            var projectDirectory = Path.Combine(solutionDirectory, "Analyzers", "test");
            return projectDirectory;
        }
    }
}
