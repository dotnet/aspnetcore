// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    public abstract class AnalyzerTestBase
    {
        public TestSource Read(string source)
        {
            if (!source.EndsWith(".cs", StringComparison.Ordinal))
            {
                source = source + ".cs";
            }

            var filePath = Path.Combine(AppContext.BaseDirectory, "TestFiles", GetType().Name, source);
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
                source = source + ".cs";
            }

            var read = Read(source);
            return AnalyzersDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { read.Source, });
        }

        public Task<Compilation> CreateCompilationAsync(string source)
        {
            return CreateProject(source).GetCompilationAsync();
        }
    }
}
