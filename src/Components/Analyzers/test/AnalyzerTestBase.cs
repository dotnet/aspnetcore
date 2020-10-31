// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    public abstract class AnalyzerTestBase
    {
        // Test files are copied to both the bin/ and publish/ folders. Use BaseDirectory on or off Helix.
        private static readonly string ProjectDirectory = AppContext.BaseDirectory;

        public TestSource Read(string source)
        {
            if (!source.EndsWith(".cs", StringComparison.Ordinal))
            {
                source += ".cs";
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
            if (!source.EndsWith(".cs", StringComparison.Ordinal))
            {
                source += ".cs";
            }

            var read = Read(source);
            return DiagnosticProject.Create(GetType().Assembly, new[] { read.Source, });
        }

        public Task<Compilation> CreateCompilationAsync(string source)
        {
            return CreateProject(source).GetCompilationAsync();
        }
    }
}
