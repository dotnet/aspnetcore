// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class WasmBuildIncrementalismTest
    {
        [Fact]
        public async Task Build_WithLinker_IsIncremental()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Act
            var thumbPrint = FileThumbPrint.CreateFolderThumbprint(project, project.BuildOutputDirectory);

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project);
                Assert.BuildPassed(result);

                var newThumbPrint = FileThumbPrint.CreateFolderThumbprint(project, project.BuildOutputDirectory);
                Assert.Equal(thumbPrint.Count, newThumbPrint.Count);
                for (var j = 0; j < thumbPrint.Count; j++)
                {
                    Assert.Equal(thumbPrint[j], newThumbPrint[j]);
                }
            }
        }

        [Fact]
        public async Task Build_SatelliteAssembliesFileIsPreserved()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            File.Move(Path.Combine(project.DirectoryPath, "Resources.ja.resx.txt"), Path.Combine(project.DirectoryPath, "Resource.ja.resx"));
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var satelliteAssemblyCacheFile = Path.Combine(project.IntermediateOutputDirectory, "blazor.satelliteasm.props");
            var satelliteAssemblyFile = Path.Combine(project.BuildOutputDirectory, "wwwroot", "_framework", "ja", "blazorwasm.resources.dll");
            var bootJson = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");

            // Assert
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project);
                Assert.BuildPassed(result);

                Verify();
            }

            // Assert - incremental builds with BuildingProject=false
            for (var i = 0; i < 3; i++)
            {
                result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:BuildingProject=false");
                Assert.BuildPassed(result);

                Verify();
            }

            void Verify()
            {
                Assert.FileExists(result, satelliteAssemblyCacheFile);
                Assert.FileExists(result, satelliteAssemblyFile);

                var bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var satelliteResources = bootJsonFile.resources.satelliteResources;
                var kvp = Assert.Single(satelliteResources);
                Assert.Equal("ja", kvp.Key);
                Assert.Equal("ja/blazorwasm.resources.dll", Assert.Single(kvp.Value).Key);
            }
        }

        [Fact]
        public async Task Build_SatelliteAssembliesFileIsCreated_IfNewFileIsAdded()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var satelliteAssemblyCacheFile = Path.Combine(project.IntermediateOutputDirectory, "blazor.satelliteasm.props");
            var satelliteAssemblyFile = Path.Combine(project.BuildOutputDirectory, "wwwroot", "_framework", "ja", "blazorwasm.resources.dll");
            var bootJson = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");

            result = await MSBuildProcessManager.DotnetMSBuild(project);
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, satelliteAssemblyCacheFile);
            Assert.FileDoesNotExist(result, satelliteAssemblyFile);
            var bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var satelliteResources = bootJsonFile.resources.satelliteResources;
            Assert.Null(satelliteResources);

            File.Move(Path.Combine(project.DirectoryPath, "Resources.ja.resx.txt"), Path.Combine(project.DirectoryPath, "Resource.ja.resx"));
            result = await MSBuildProcessManager.DotnetMSBuild(project);
            Assert.BuildPassed(result);

            Assert.FileExists(result, satelliteAssemblyCacheFile);
            Assert.FileExists(result, satelliteAssemblyFile);
            bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            satelliteResources = bootJsonFile.resources.satelliteResources;
            var kvp = Assert.Single(satelliteResources);
            Assert.Equal("ja", kvp.Key);
            Assert.Equal("ja/blazorwasm.resources.dll", Assert.Single(kvp.Value).Key);
        }

        [Fact]
        public async Task Build_SatelliteAssembliesFileIsDeleted_IfAllSatelliteFilesAreRemoved()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            File.Move(Path.Combine(project.DirectoryPath, "Resources.ja.resx.txt"), Path.Combine(project.DirectoryPath, "Resource.ja.resx"));

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var satelliteAssemblyCacheFile = Path.Combine(project.IntermediateOutputDirectory, "blazor.satelliteasm.props");
            var satelliteAssemblyFile = Path.Combine(project.BuildOutputDirectory, "wwwroot", "_framework", "ja", "blazorwasm.resources.dll");
            var bootJson = Path.Combine(project.DirectoryPath, project.BuildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");

            result = await MSBuildProcessManager.DotnetMSBuild(project);
            Assert.BuildPassed(result);

            Assert.FileExists(result, satelliteAssemblyCacheFile);
            Assert.FileExists(result, satelliteAssemblyFile);
            var bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var satelliteResources = bootJsonFile.resources.satelliteResources;
            var kvp = Assert.Single(satelliteResources);
            Assert.Equal("ja", kvp.Key);
            Assert.Equal("ja/blazorwasm.resources.dll", Assert.Single(kvp.Value).Key);


            File.Delete(Path.Combine(project.DirectoryPath, "Resource.ja.resx"));
            result = await MSBuildProcessManager.DotnetMSBuild(project);
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, satelliteAssemblyCacheFile);
            bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            satelliteResources = bootJsonFile.resources.satelliteResources;
            Assert.Null(satelliteResources);
        }
    }
}
