// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using static Microsoft.AspNetCore.Components.WebAssembly.Build.WebAssemblyRuntimePackage;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class BuildLazyLoadTest
    {
        [Fact]
        public async Task Build_LazyLoadExplicitAssembly_Debug_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include='RazorClassLibrary.dll' />
</ItemGroup>
");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            var assemblies = bootJson.resources.assembly;

            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RazorClassLibrary.dll", dynamicAssemblies.Keys);
            Assert.DoesNotContain("RazorClassLibrary.dll", assemblies.Keys);

            // App assembly should not be lazy loaded
            Assert.DoesNotContain("standalone.dll", dynamicAssemblies.Keys);
            Assert.Contains("standalone.dll", assemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadExplicitAssembly_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include='RazorClassLibrary.dll' />
</ItemGroup>
");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            var assemblies = bootJson.resources.assembly;

            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RazorClassLibrary.dll", dynamicAssemblies.Keys);
            Assert.DoesNotContain("RazorClassLibrary.dll", assemblies.Keys);

            // App assembly should not be lazy loaded
            Assert.DoesNotContain("standalone.dll", dynamicAssemblies.Keys);
            Assert.Contains("standalone.dll", assemblies.Keys);
        }

                [Fact]
        public async Task Publish_LazyLoadExplicitAssembly_Debug_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include='RazorClassLibrary.dll' />
</ItemGroup>
");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            var publishDirectory = project.PublishOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, publishDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, publishDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(publishDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            var assemblies = bootJson.resources.assembly;

            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RazorClassLibrary.dll", dynamicAssemblies.Keys);
            Assert.DoesNotContain("RazorClassLibrary.dll", assemblies.Keys);

            // App assembly should not be lazy loaded
            Assert.DoesNotContain("standalone.dll", dynamicAssemblies.Keys);
            Assert.Contains("standalone.dll", assemblies.Keys);
        }

        [Fact]
        public async Task Publish_LazyLoadExplicitAssembly_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <BlazorWebAssemblyLazyLoad Include='RazorClassLibrary.dll' />
</ItemGroup>
");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            var publishDirectory = project.PublishOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, publishDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, publishDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(publishDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            var assemblies = bootJson.resources.assembly;

            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RazorClassLibrary.dll", dynamicAssemblies.Keys);
            Assert.DoesNotContain("RazorClassLibrary.dll", assemblies.Keys);

            // App assembly should not be lazy loaded
            Assert.DoesNotContain("standalone.dll", dynamicAssemblies.Keys);
            Assert.Contains("standalone.dll", assemblies.Keys);
        }

        private static GenerateBlazorBootJson.BootJsonData ReadBootJsonData(MSBuildResult result, string path)
        {
            return JsonSerializer.Deserialize<GenerateBlazorBootJson.BootJsonData>(
                File.ReadAllText(Path.Combine(result.Project.DirectoryPath, path)),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
