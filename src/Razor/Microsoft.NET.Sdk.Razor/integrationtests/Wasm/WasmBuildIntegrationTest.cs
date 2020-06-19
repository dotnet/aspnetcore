// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class WasmBuildIntegrationTest
    {
        private static readonly string DotNetJsFileName = "dotnet.js";

        [Fact]
        public async Task Build_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "RazorClassLibrary.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.pdb");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "RazorClassLibrary.pdb");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorwasm.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine(project.TargetFramework, "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.GetFullPath(Path.Combine(project.SolutionPath, "razorclasslibrary", "wwwroot")));
        }

        [Fact]
        public async Task Build_InRelease_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "RazorClassLibrary.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.pdb");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "RazorClassLibrary.pdb");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorwasm.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine(project.TargetFramework, "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.GetFullPath(Path.Combine(project.SolutionPath, "razorclasslibrary", "wwwroot")));
        }

        [Fact]
        public async Task Build_ProducesBootJsonDataWithExpectedContent()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";
            var wwwroot = Path.Combine(project.DirectoryPath, "wwwroot");
            File.WriteAllText(Path.Combine(wwwroot, "appsettings.json"), "Default settings");
            File.WriteAllText(Path.Combine(wwwroot, "appsettings.development.json"), "Development settings");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains(DotNetJsFileName, runtime);
            Assert.Contains("dotnet.wasm", runtime);

            var assemblies = bootJsonData.resources.assembly.Keys;
            Assert.Contains("blazorwasm.dll", assemblies);
            Assert.Contains("RazorClassLibrary.dll", assemblies);
            Assert.Contains("System.Text.Json.dll", assemblies);

            var pdb = bootJsonData.resources.pdb.Keys;
            Assert.Contains("blazorwasm.pdb", pdb);
            Assert.Contains("RazorClassLibrary.pdb", pdb);

            Assert.Null(bootJsonData.resources.satelliteResources);

            Assert.Contains("appsettings.json", bootJsonData.config);
            Assert.Contains("appsettings.development.json", bootJsonData.config);
        }

        [Fact]
        public async Task Build_InRelease_ProducesBootJsonDataWithExpectedContent()
        {
            // Arrange
            var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains(DotNetJsFileName, runtime);
            Assert.Contains("dotnet.wasm", runtime);

            var assemblies = bootJsonData.resources.assembly.Keys;
            Assert.Contains("blazorwasm.dll", assemblies);
            Assert.Contains("RazorClassLibrary.dll", assemblies);
            Assert.Contains("System.Text.Json.dll", assemblies);

            var pdb = bootJsonData.resources.pdb.Keys;
            Assert.Contains("blazorwasm.pdb", pdb);
            Assert.Contains("RazorClassLibrary.pdb", pdb);
            Assert.Null(bootJsonData.resources.satelliteResources);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/22975")]
        public async Task Build_WithBlazorEnableTimeZoneSupportDisabled_DoesNotCopyTimeZoneInfo()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";
            project.AddProjectFileContent(
@"
<PropertyGroup>
    <BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains("dotnet.wasm", runtime);
            Assert.DoesNotContain("dotnet.timezones.dat", runtime);

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.timezones.dat");
        }

        [Fact]
        public async Task Build_Hosted_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "blazorwasm.dll");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorhosted.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("net5.0", "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.Combine("razorclasslibrary", "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.Combine("blazorwasm", "wwwroot"));
        }

        [Fact]
        [QuarantinedTest]
        public async Task Build_SatelliteAssembliesAreCopiedToBuildOutput()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary", "classlibrarywithsatelliteassemblies" });
            project.AddProjectFileContent(
@"
<PropertyGroup>
    <DefineConstants>$(DefineConstants);REFERENCE_classlibrarywithsatelliteassemblies</DefineConstants>
</PropertyGroup>
<ItemGroup>
    <ProjectReference Include=""..\classlibrarywithsatelliteassemblies\classlibrarywithsatelliteassemblies.csproj"" />
</ItemGroup>");
            var resxfileInProject = Path.Combine(project.DirectoryPath, "Resources.ja.resx.txt");
            File.Move(resxfileInProject, Path.Combine(project.DirectoryPath, "Resource.ja.resx"));

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "classlibrarywithsatelliteassemblies.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");
        }

        [Fact]
        public async Task Build_WithCustomOutputPath_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });

            project.AddDirectoryBuildContent(
@"<PropertyGroup>
    <BaseOutputPath>$(MSBuildThisFileDirectory)build\bin\</BaseOutputPath>
    <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)build\obj\</BaseIntermediateOutputPath>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");
            Assert.BuildPassed(result);
        }

        private static BootJsonData ReadBootJsonData(MSBuildResult result, string path)
        {
            return JsonSerializer.Deserialize<BootJsonData>(
                File.ReadAllText(Path.Combine(result.Project.DirectoryPath, path)),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
