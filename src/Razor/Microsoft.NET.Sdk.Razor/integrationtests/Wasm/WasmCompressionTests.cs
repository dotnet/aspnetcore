// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class WasmCompressionTests
    {
        [Fact]
        public async Task Publish_UpdatesFilesWhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            // Act
            var mainAppDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);
            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll.br");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var blazorBootJson = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var blazorBootJsonThumbPrint = FileThumbPrint.Create(blazorBootJson);
            var blazorBootJsonCompressed = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json.br");
            var blazorBootJsonCompressedThumbPrint = FileThumbPrint.Create(blazorBootJsonCompressed);

            var programFile = Path.Combine(project.DirectoryPath, "..", "blazorwasm", "Program.cs");
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
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish", args: "/p:BlazorWebAssemblyEnableLinking=false");

            Assert.BuildPassed(result);

            // Act
            var mainAppDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            var mainAppDllThumbPrint = FileThumbPrint.Create(mainAppDll);

            var mainAppCompressedDll = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll.br");
            var mainAppCompressedDllThumbPrint = FileThumbPrint.Create(mainAppCompressedDll);

            var programFile = Path.Combine(project.DirectoryPath, "..", "blazorwasm", "Program.cs");
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
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine("..", "blazorwasm", project.IntermediateOutputDirectory, "compressed");
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
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish", args: "/p:BlazorWebAssemblyEnableLinking=false");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var compressedFilesFolder = Path.Combine("..", "blazorwasm", project.IntermediateOutputDirectory, "compressed");
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
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "publish");

            Assert.BuildPassed(result);

            var extensions = new[] { ".dll", ".js", ".pdb", ".wasm", ".map", ".json", ".dat" };

            // Act
            var frameworkFilesPath = Path.Combine(project.DirectoryPath, project.PublishOutputDirectory, "wwwroot", "_framework");

            foreach (var file in Directory.EnumerateFiles(frameworkFilesPath, "*", new EnumerationOptions {  RecurseSubdirectories = true, }))
            {
                var extension = Path.GetExtension(file);
                if (extension != ".br" && extension != ".gz")
                {
                    Assert.FileExists(result, file + ".br");
                }
            }
        }
    }
}
