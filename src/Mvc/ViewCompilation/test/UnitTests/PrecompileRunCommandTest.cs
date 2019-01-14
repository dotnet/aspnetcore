// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class PrecompileRunCommandTest
    {
        [Fact]
        public void RunPrintsHelp_WhenHelpOptionIsSpecified()
        {
            // Arrange
            var expected =
$@"Microsoft Razor Precompilation Utility {GetToolVersion()}

Usage: razor-precompile [arguments] [options]

Arguments:
  project  The path to the project file.

Options:
  -?|-h|--help                  Show help information
  --output-path                 Path to the emit the precompiled assembly to.
  --application-name            Name of the application to produce precompiled assembly for.
  --configure-compilation-type  Type with Configure method
  --content-root                The application's content root.
  --embed-view-sources          Embed view sources as resources in the generated assembly.
  --key-file                    Strong name key path.
  --delay-sign                  Determines if the precompiled view assembly is to be delay signed.
  --public-sign                 Determines if the precompiled view assembly is to be public signed.
  --file                        Razor files to compile.";

            var args = new[]
            {
                "--help"
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(expected, result.Out.Trim(), ignoreLineEndingDifferences: true);
            Assert.Empty(result.Error);
        }

        [Fact]
        public void Run_PrintsHelpWhenInvalidOptionsAreSpecified()
        {
            // Arrange
            var expectedOut = @"Specify --help for a list of available options and commands.";
            var expectedError = @"Unrecognized option '--bad-option'";
            var args = new[]
            {
                "--bad-option"
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Equal(expectedOut, result.Out.Trim());
            Assert.Equal(
                expectedError,
                result.Error.Split(new[] { Environment.NewLine }, StringSplitOptions.None).First());
        }

        [Fact]
        public void Run_PrintsErrorWhenArgumentIsMissing()
        {
            // Arrange
            var expectedError = @"Project path not specified.";
            var args = new string[0];

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expectedError, result.Error.Trim());
        }

        [Fact]
        public void Run_PrintsErrorWhenOutputPathOptionIsMissing()
        {
            // Arrange
            var expectedError = @"Option --output-path does not specify a value.";
            var args = new[]
            {
                Directory.GetCurrentDirectory(),
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expectedError, result.Error.Trim());
        }

        [Fact]
        public void Run_PrintsErrorWhenApplicationNameOptionIsMissing()
        {
            // Arrange
            var expectedError = @"Option --application-name does not specify a value.";
            var args = new[]
            {
                Directory.GetCurrentDirectory(),
                "--output-path",
                Directory.GetCurrentDirectory(),
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expectedError, result.Error.Trim());
        }

        [Fact]
        public void Run_PrintsErrorWhenContentRootOptionIsMissing()
        {
            // Arrange
            var expectedError = @"Option --content-root does not specify a value.";
            var args = new[]
            {
                Directory.GetCurrentDirectory(),
                "--output-path",
                Directory.GetCurrentDirectory(),
                "--application-name",
                "TestApplicationName",
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expectedError, result.Error.Trim());
        }

        [Fact]
        public void EmitAssembly_DoesNotWriteAssembliesToDisk_IfCompilationFails()
        {
            // Arrange
            var assemblyDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var assemblyPath = Path.Combine(assemblyDirectory, "out.dll");
            var precompileRunCommand = new PrecompileRunCommand();
            var syntaxTree = CSharpSyntaxTree.ParseText("using Microsoft.DoestNotExist");
            var compilation = CSharpCompilation.Create("Test.dll", new[] { syntaxTree });

            // Act
            var emitResult = precompileRunCommand.EmitAssembly(
                compilation,
                new EmitOptions(),
                assemblyPath,
                new ResourceDescription[0]);

            // Assert
            Assert.False(emitResult.Success);
            Assert.False(Directory.Exists(assemblyDirectory));
            Assert.False(File.Exists(assemblyPath));
        }

        private static string GetToolVersion()
        {
            return typeof(Program)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
        }

        private class ExecuteResult
        {
            public string Out { get; set; }

            public string Error { get; set; }

            public int ExitCode { get; set; }
        }

        private ExecuteResult Execute(string[] args)
        {
            using (var outputWriter = new StringWriter())
            using (var errorWriter = new StringWriter())
            {
                var app = new PrecompilationApplication(typeof(Program))
                {
                    Out = outputWriter,
                    Error = errorWriter,
                };
                new PrecompileRunCommand().Configure(app);
                var exitCode = app.Execute(args);

                return new ExecuteResult
                {
                    ExitCode = exitCode,
                    Out = outputWriter.ToString(),
                    Error = errorWriter.ToString(),
                };
            }
        }
    }
}
