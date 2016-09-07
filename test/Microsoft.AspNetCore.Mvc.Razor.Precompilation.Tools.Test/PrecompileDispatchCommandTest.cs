// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal;
using Microsoft.AspNetCore.Mvc.Razor.Precompilation.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Tools
{
    public class PrecompileDispatchCommandTest
    {
        [Fact]
        public void RunPrintsHelp_WhenHelpOptionIsSpecified()
        {
            // Arrange
            var expected =
$@"Microsoft Razor Precompilation Utility {GetToolVersion()}

Usage: razor-precompile [arguments] [options]

Arguments:
  project  The path to the project (project folder or project.json) with precompilation.

Options:
  -?|-h|--help                  Show help information
  --configure-compilation-type  Type with Configure method
  --content-root                The application's content root.
  --embed-view-sources          Embed view sources as resources in the generated assembly.
  -f|--framework                Target Framework
  -c|--configuration            Configuration
  -o|--output-path              Output path.";
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
        public void RunPrintsHelp_WhenInvalidOptionsAreSpecified()
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
        public void RunPrintsError_IfFrameworkIfNotSpecified()
        {
            // Arrange
            var expected = "Option -f|--framework does not have a value.";
            var args = new string[0];

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expected, result.Error.Trim());
        }

        [Fact]
        public void RunPrintsError_IfOutputPathIfNotSpecified()
        {
            // Arrange
            var expected = "Option -o|--output-path does not have a value.";
            var args = new[]
            {
                "-f",
                "framework"
            };

            // Act
            var result = Execute(args);

            // Assert
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Out);
            Assert.Equal(expected, result.Error.Trim());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetProjectPath_ReturnsCurrentDirectoryIfArgumentIsNullOrEmpty(string projectPath)
        {
            // Act
            var actual = PrecompileDispatchCommand.GetProjectPath(projectPath);

            // Assert
            Assert.Equal(Directory.GetCurrentDirectory(), actual);
        }

        public static TheoryData GetProjectPath_ReturnsArgumentIfNotNullOrEmptyData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { "", Directory.GetCurrentDirectory() },
                    { "project.json", Directory.GetCurrentDirectory() },
                    { Path.GetTempPath(), Path.GetTempPath() },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetProjectPath_ReturnsArgumentIfNotNullOrEmptyData))]
        public void GetProjectPath_ReturnsArgumentIfNotNullOrEmpty(string projectPath, string expected)
        {
            // Act
            var actual = PrecompileDispatchCommand.GetProjectPath(projectPath);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetProjectPath_ThrowsIfDirectoryDoesNotExist()
        {
            // Arrange
            var nonExistent = Path.GetRandomFileName();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => PrecompileDispatchCommand.GetProjectPath(nonExistent));
            Assert.Equal($"Could not find directory {Path.GetFullPath(nonExistent)}.", ex.Message);
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
                new PrecompileDispatchCommand().Configure(app);
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
