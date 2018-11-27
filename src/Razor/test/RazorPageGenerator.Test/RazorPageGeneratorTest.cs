// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace RazorPageGenerator.Test
{
    public class RazorPageGeneratorTest
    {

#if GENERATE_BASELINES
        private static readonly bool GenerateBaselines = true;
#else
        private static readonly bool GenerateBaselines = false;
#endif


        [Fact]
        public void Generator_ReturnsNonZeroExitCode_WhenArgsAreNotSupplied()
        {
            // Arrange
            var args = new string[0];

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void Generator_GeneratesCodeForFilesIntheViewsDirectory()
        {
            // Arrange
            var projectDirectory = TestProject.GetProjectDirectory(GetType());
            var projectEngine = Program.CreateProjectEngine("Microsoft.AspNetCore.TestGenerated", projectDirectory);

            // Act
            var results = Program.MainCore(projectEngine, projectDirectory);

            // Assert
            Assert.Collection(results,
                result =>
                {
                    var expectedPath = Path.Combine(projectDirectory, "TestFiles", "Views", "TestView.Designer.cs");
                    var expectedFile = Path.ChangeExtension(expectedPath, ".expected.cs");
                    Assert.Equal(expectedPath, result.FilePath);

                    var generatedCode = result.GeneratedCode
                        .Replace("\r", "")
                        .Replace("\n", "\r\n")
                        .Replace("\\r", "")
                        .Replace("\\n", "\\r\\n");

                    if (GenerateBaselines)
                    {
                        File.WriteAllText(expectedFile, generatedCode);
                    }
                    else
                    {
                        var expectedContent = File.ReadAllText(expectedFile).Replace("\r", "").Replace("\n", "\r\n");
                        Assert.Equal(expectedContent, generatedCode);
                    }
                });
        }
    }
}
