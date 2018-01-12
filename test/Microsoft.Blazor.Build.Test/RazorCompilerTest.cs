// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Build.Core.RazorCompilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.Blazor.Build.Test
{
    public class RazorCompilerTest
    {
        [Fact]
        public void RejectsInvalidClassName()
        {
            // Arrange/Act
            var result = CompileToCSharp(
                "x:\\dir\\subdir\\Filename with spaces.cshtml",
                "ignored code",
                "ignored namespace");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal(RazorCompilerDiagnostic.DiagnosticType.Error, item.Type);
                    Assert.StartsWith($"Invalid name 'Filename with spaces'", item.Message);
                });
        }

        [Fact]
        public void CreatesClassWithCorrectNameAndNamespace()
        {
            // Arrange/Act
            var result = CompileToAssembly(
                "x:\\dir\\subdir\\Filename.cshtml",
                "{* No code *}",
                "MyCompany.MyNamespace");

            // Assert
            Assert.Empty(result.Diagnostics);
            Assert.Collection(result.Assembly.GetTypes(),
                type =>
                {
                    Assert.Equal("Filename", type.Name);
                    Assert.Equal("MyCompany.MyNamespace", type.Namespace);
                });
        }

        private static CompileToAssemblyResult CompileToAssembly(string cshtmlFilename, string cshtmlContent, string outputNamespace)
        {
            var csharpResult = CompileToCSharp(cshtmlFilename, cshtmlContent, outputNamespace);
            if (csharpResult.Diagnostics.Any())
            {
                var diagnosticsLog = string.Join(Environment.NewLine,
                    csharpResult.Diagnostics.Select(d => d.FormatForConsole()).ToArray());
                throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
            }

            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(csharpResult.Code)
            };
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
            };
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var assemblyName = "TestAssembly" + Guid.NewGuid().ToString("N");
            var compilation = CSharpCompilation.Create(assemblyName,
                syntaxTrees,
                references,
                options);

            using (var peStream = new MemoryStream())
            {
                compilation.Emit(peStream);

                return new CompileToAssemblyResult
                {
                    Diagnostics = compilation.GetDiagnostics(),
                    VerboseLog = csharpResult.VerboseLog,
                    Assembly = Assembly.Load(peStream.ToArray())
                };
            }
        }

        private static CompileToCSharpResult CompileToCSharp(string cshtmlFilename, string cshtmlContent, string outputNamespace)
        {
            using (var resultStream = new MemoryStream())
            using (var resultWriter = new StreamWriter(resultStream))
            using (var verboseLogStream = new MemoryStream())
            using (var verboseWriter = new StreamWriter(verboseLogStream))
            using (var inputReader = new StringReader(cshtmlContent))
            {
                var diagnostics = new RazorCompiler().CompileSingleFile(
                    cshtmlFilename,
                    inputReader,
                    outputNamespace,
                    resultWriter,
                    verboseWriter);

                resultWriter.Flush();
                verboseWriter.Flush();
                return new CompileToCSharpResult
                {
                    Code = Encoding.UTF8.GetString(resultStream.ToArray()),
                    VerboseLog = Encoding.UTF8.GetString(verboseLogStream.ToArray()),
                    Diagnostics = diagnostics
                };
            }
        }

        private class CompileToCSharpResult
        {
            public string Code { get; set; }
            public string VerboseLog { get; set; }
            public IEnumerable<RazorCompilerDiagnostic> Diagnostics { get; set; }
        }

        private class CompileToAssemblyResult
        {
            public Assembly Assembly { get; set; }
            public string VerboseLog { get; set; }
            public IEnumerable<Diagnostic> Diagnostics { get; set; }
        }
    }
}
