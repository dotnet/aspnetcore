// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class BuildCompressionTests
    {
        [Fact]
        public async Task Build_WithLinkerAndCompression_IsIncremental()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine(project.IntermediateOutputDirectory, "compressed");
            var thumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project);
                Assert.BuildPassed(result);

                var newThumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);
                Assert.Equal(thumbPrint.Count, newThumbPrint.Count);
                for (var j = 0; j < thumbPrint.Count; j++)
                {
                    Assert.Equal(thumbPrint[j], newThumbPrint[j]);
                }
            }
        }

        [Fact]
        public async Task Build_WithoutLinkerAndCompression_IsIncremental()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorWebAssemblyEnableLinking=false");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine(project.IntermediateOutputDirectory, "compressed");
            var thumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorWebAssemblyEnableLinking=false");
                Assert.BuildPassed(result);

                var newThumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);
                Assert.Equal(thumbPrint.Count, newThumbPrint.Count);
                for (var j = 0; j < thumbPrint.Count; j++)
                {
                    Assert.Equal(thumbPrint[j], newThumbPrint[j]);
                }
            }
        }

        [Fact]
        public async Task Build_CompressesAllFrameworkFiles()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var extensions = new[] { ".dll", ".js", ".pdb", ".wasm", ".map", ".json", ".dat" };
            // Act
            var compressedFilesPath = Path.Combine(
                project.DirectoryPath,
                project.IntermediateOutputDirectory,
                "compressed",
                "_framework");
            var compressedFiles = Directory.EnumerateFiles(
                compressedFilesPath,
                "*",
                SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f) == ".gz")
                .Select(f => Path.GetRelativePath(compressedFilesPath, f[0..^3]))
                .OrderBy(f => f)
                .ToArray();

            var frameworkFilesPath = Path.Combine(
                project.DirectoryPath,
                project.BuildOutputDirectory,
                "wwwroot",
                "_framework");
            var frameworkFiles = Directory.EnumerateFiles(
                frameworkFilesPath,
                "*",
                SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f)))
                .Select(f => Path.GetRelativePath(frameworkFilesPath, f))
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(frameworkFiles.Length, compressedFiles.Length);
            Assert.Equal(frameworkFiles, compressedFiles);

            var brotliFiles = Directory.EnumerateFiles(
                compressedFilesPath,
                "*",
                SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f) == ".br")
                .Select(f => Path.GetRelativePath(compressedFilesPath, f[0..^3]))
                .OrderBy(f => f)
                .ToArray();

            // We don't compress things with brotli at build time
            Assert.Empty(brotliFiles);
        }

        [Fact]
        public async Task Build_DisabledCompression_DoesNotCompressFiles()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });

            // Act
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorEnableCompression=false");

            //Assert
            Assert.BuildPassed(result);

            var compressedFilesPath = Path.Combine(
                project.DirectoryPath,
                project.IntermediateOutputDirectory,
                "compressed");

            Assert.False(Directory.Exists(compressedFilesPath));
        }

        [Fact]
        public async Task Publish_WithLinkerAndCompression_IsIncremental()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine("..", "standalone", project.IntermediateOutputDirectory, "compressed");
            var thumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project);
                Assert.BuildPassed(result);

                var newThumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);
                Assert.Equal(thumbPrint.Count, newThumbPrint.Count);
                for (var j = 0; j < thumbPrint.Count; j++)
                {
                    Assert.Equal(thumbPrint[j], newThumbPrint[j]);
                }
            }
        }

        [Fact]
        public async Task Publish_WithoutLinkerAndCompression_IsIncremental()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish", args: "/p:BlazorWebAssemblyEnableLinking=false");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine("..", "standalone", project.IntermediateOutputDirectory, "compressed");
            var thumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorWebAssemblyEnableLinking=false");
                Assert.BuildPassed(result);

                var newThumbPrint = FileThumbPrint.CreateFolderThumbprint(project, compressedFilesFolder);
                Assert.Equal(thumbPrint.Count, newThumbPrint.Count);
                for (var j = 0; j < thumbPrint.Count; j++)
                {
                    Assert.Equal(thumbPrint[j], newThumbPrint[j]);
                }
            }
        }

        [Fact]
        public async Task Publish_CompressesAllFrameworkFiles()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var extensions = new[] { ".dll", ".js", ".pdb", ".wasm", ".map", ".json", ".dat" };
            // Act
            var compressedFilesPath = Path.Combine(
                project.DirectoryPath,
                "..",
                "standalone",
                project.IntermediateOutputDirectory,
                "compressed",
                "_framework");
            var compressedFiles = Directory.EnumerateFiles(
                compressedFilesPath,
                "*",
                SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f) == ".br")
                .Select(f => Path.GetRelativePath(compressedFilesPath, f[0..^3]))
                .OrderBy(f => f)
                .ToArray();

            var frameworkFilesPath = Path.Combine(
                project.DirectoryPath,
                project.BuildOutputDirectory,
                "wwwroot",
                "_framework");
            var frameworkFiles = Directory.EnumerateFiles(
                frameworkFilesPath,
                "*",
                SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f)))
                .Select(f => Path.GetRelativePath(frameworkFilesPath, f))
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(frameworkFiles.Length, compressedFiles.Length);
            Assert.Equal(frameworkFiles, compressedFiles);
        }
    }
}
