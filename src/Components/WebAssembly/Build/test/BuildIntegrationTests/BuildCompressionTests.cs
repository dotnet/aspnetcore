// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public async Task Build_WithLinkerAndCompression_UpdatesFilesWhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var mainAppDll = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll.gz");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var blazorBootJson = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var blazorBootJsonThumbPrint = FileThumbPrint.Create(blazorBootJson);
            var blazorBootJsonCompressed = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json.gz");
            var blazorBootJsonCompressedThumbPrint = FileThumbPrint.Create(blazorBootJsonCompressed);

            // Act
            var programFile = Path.Combine(project.DirectoryPath, "Program.cs");
            var programFileContents = File.ReadAllText(programFile);
            File.WriteAllText(programFile, programFileContents.Replace("args", "arguments"));
            result = await MSBuildProcessManager.DotnetMSBuild(project);

            // Assert
            Assert.BuildPassed(result);
            var newMainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var newMainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);
            var newBlazorBootJsonThumbPrint = FileThumbPrint.Create(blazorBootJson);
            var newBlazorBootJsonCompressedThumbPrint = FileThumbPrint.Create(blazorBootJsonCompressed);

            Assert.NotEqual(mainAppDllThumbPrint, newMainAppDllThumbPrint);
            Assert.NotEqual(mainAppCompressedDllThumbPrint, newMainAppCompressedDllThumbPrint);

            Assert.NotEqual(blazorBootJsonThumbPrint, newBlazorBootJsonThumbPrint);
            Assert.NotEqual(blazorBootJsonCompressedThumbPrint, newBlazorBootJsonCompressedThumbPrint);
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
        public async Task Build_WithoutLinkerAndCompression_UpdatesFilesWhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorWebAssemblyEnableLinking=false");

            // Act
            var mainAppDll = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);

            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll.gz");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var programFile = Path.Combine(project.DirectoryPath, "Program.cs");
            var programFileContents = File.ReadAllText(programFile);
            File.WriteAllText(programFile, programFileContents.Replace("args", "arguments"));

            // Assert
            result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BlazorWebAssemblyEnableLinking=false");
            Assert.BuildPassed(result);
            var newMainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var newMainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            Assert.NotEqual(mainAppDllThumbPrint, newMainAppDllThumbPrint);
            Assert.NotEqual(mainAppCompressedDllThumbPrint, newMainAppCompressedDllThumbPrint);
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
        public async Task Publish_WithLinkerAndCompression_UpdatesFilesWhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary" });
            project.TargetFramework = "netcoreapp3.1";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            // Act
            var mainAppDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll.br");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var blazorBootJson = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var blazorBootJsonThumbPrint = FileThumbPrint.Create(blazorBootJson);
            var blazorBootJsonCompressed = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json.br");
            var blazorBootJsonCompressedThumbPrint = FileThumbPrint.Create(blazorBootJsonCompressed);

            var programFile = Path.Combine(project.DirectoryPath, "..", "standalone", "Program.cs");
            var programFileContents = File.ReadAllText(programFile);
            File.WriteAllText(programFile, programFileContents.Replace("args", "arguments"));

            // Assert
            result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");
            Assert.BuildPassed(result);
            var newMainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var newMainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);
            var newBlazorBootJsonThumbPrint = FileThumbPrint.Create(blazorBootJson);
            var newBlazorBootJsonCompressedThumbPrint = FileThumbPrint.Create(blazorBootJsonCompressed);

            Assert.NotEqual(mainAppDllThumbPrint, newMainAppDllThumbPrint);
            Assert.NotEqual(mainAppCompressedDllThumbPrint, newMainAppCompressedDllThumbPrint);

            Assert.NotEqual(blazorBootJsonThumbPrint, newBlazorBootJsonThumbPrint);
            Assert.NotEqual(blazorBootJsonCompressedThumbPrint, newBlazorBootJsonCompressedThumbPrint);
        }

        [Fact]
        public async Task Publish_WithoutLinkerAndCompression_UpdatesFilesWhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary" });
            project.TargetFramework = "netcoreapp3.1";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish", args: "/p:BlazorWebAssemblyEnableLinking=false");

            Assert.BuildPassed(result);

            // Act
            var mainAppDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);

            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll.br");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var programFile = Path.Combine(project.DirectoryPath, "..", "standalone", "Program.cs");
            var programFileContents = File.ReadAllText(programFile);
            File.WriteAllText(programFile, programFileContents.Replace("args", "arguments"));

            // Assert
            result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish", args: "/p:BlazorWebAssemblyEnableLinking=false");
            Assert.BuildPassed(result);
            var newMainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var newMainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            Assert.NotEqual(mainAppDllThumbPrint, newMainAppDllThumbPrint);
            Assert.NotEqual(mainAppCompressedDllThumbPrint, newMainAppCompressedDllThumbPrint);
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
