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
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithnodependencies" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithnodependencies\RclWithNoDeps.csproj'/>
</ItemGroup>
<ItemGroup>
    <BlazorLazyLoad Include='RclWithNoDeps.dll' />
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RclWithNoDeps.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RclWithNoDeps.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadRCL_Debug_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithnodependencies" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithnodependencies\RclWithNoDeps.csproj' BlazorLazyLoad='true'/>
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RclWithNoDeps.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RclWithNoDeps.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadRCLWithDependencies_Debug_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithpackages" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithpackages\rclwithpackages.csproj' BlazorLazyLoad='true'/>
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "rclwithpackages.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);

            // Packages that are dependencies of rclwithpackage and rclwithpackages itself are lazy-loaded
            Assert.Contains("rclwithpackages.dll", dynamicAssemblies.Keys);
            Assert.Contains("Newtonsoft.Json.dll", dynamicAssemblies.Keys);

            // Dependencies of rclwithpackages that are also dependencies of
            // non-lazy loaded components should not be lazy loaded
            Assert.DoesNotContain("Microsoft.AspNetCore.Components.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadPackage_Debug_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <PackageReference Include='Newtonsoft.Json' Version='12.0.3' BlazorLazyLoad='true'/>
    <PackageReference Include='Polly' Version='7.2.1' BlazorLazyLoad='true' />
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Newtonsoft.Json.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Polly.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("Newtonsoft.Json.dll", dynamicAssemblies.Keys);
            Assert.Contains("Polly.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadExplicitAssembly_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithnodependencies" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithnodependencies\RclWithNoDeps.csproj'/>
</ItemGroup>
<ItemGroup>
    <BlazorLazyLoad Include='RclWithNoDeps.dll' />
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            // Verify that a blazor.boot.json file has been created
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            // And that the assembly is in the output
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RclWithNoDeps.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            // And that it has been labelled as a dynamic assembly in the boot.json
            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RclWithNoDeps.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadRCL_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithnodependencies" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithnodependencies\RclWithNoDeps.csproj' BlazorLazyLoad='true'/>
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RclWithNoDeps.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("RclWithNoDeps.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadRCLWithDependencies_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "rclwithpackages" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <ProjectReference Include='..\rclwithpackages\rclwithpackages.csproj' BlazorLazyLoad='true'/>
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "rclwithpackages.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);

            // Packages that are dependencies of rclwithpackage and rclwithpackages itself are lazy-loaded
            Assert.Contains("rclwithpackages.dll", dynamicAssemblies.Keys);
            Assert.Contains("Newtonsoft.Json.dll", dynamicAssemblies.Keys);

            // Dependencies of rclwithpackages that are also dependencies of
            // non-lazy loaded components should not be lazy loaded
            Assert.DoesNotContain("Microsoft.AspNetCore.Components.dll", dynamicAssemblies.Keys);
        }

        [Fact]
        public async Task Build_LazyLoadPackage_Release_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";

            project.AddProjectFileContent(
@"
<ItemGroup>
    <PackageReference Include='Newtonsoft.Json' Version='12.0.3' BlazorLazyLoad='true'/>
    <PackageReference Include='Polly' Version='7.2.1' BlazorLazyLoad='true' />
</ItemGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Newtonsoft.Json.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Polly.dll");

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var dynamicAssemblies = bootJson.resources.dynamicAssembly;
            Assert.NotNull(dynamicAssemblies);
            Assert.Contains("Newtonsoft.Json.dll", dynamicAssemblies.Keys);
            Assert.Contains("Polly.dll", dynamicAssemblies.Keys);
        }

        private static GenerateBlazorBootJson.BootJsonData ReadBootJsonData(MSBuildResult result, string path)
        {
            return JsonSerializer.Deserialize<GenerateBlazorBootJson.BootJsonData>(
                File.ReadAllText(Path.Combine(result.Project.DirectoryPath, path)),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
