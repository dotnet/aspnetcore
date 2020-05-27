// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.AspNetCore.Components.WebAssembly.Build.WebAssemblyRuntimePackage;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class BuildIntegrationTest
    {
        [Fact]
        public async Task Build_WithDefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.timezones.dat");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.pdb");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.pdb");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "standalone.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("netstandard2.1", "wwwroot"));
        }

        [Fact]
        public async Task Build_InRelease_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.timezones.dat");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.pdb");
            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "RazorClassLibrary.pdb");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "standalone.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("netstandard2.1", "wwwroot"));
        }

        [Fact]
        public async Task Build_ProducesBootJsonDataWithExpectedContent()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains(DotNetJsFileName, runtime);
            Assert.Contains("dotnet.wasm", runtime);
            Assert.Contains("dotnet.timezones.dat", runtime);

            var assemblies = bootJsonData.resources.assembly.Keys;
            Assert.Contains("standalone.dll", assemblies);
            Assert.Contains("RazorClassLibrary.dll", assemblies);
            Assert.Contains("Microsoft.Extensions.Logging.Abstractions.dll", assemblies);

            var pdb = bootJsonData.resources.pdb.Keys;
            Assert.Contains("standalone.pdb", pdb);
            Assert.Contains("RazorClassLibrary.pdb", pdb);

            Assert.Null(bootJsonData.resources.satelliteResources);
        }

        [Fact]
        public async Task Build_InRelease_ProducesBootJsonDataWithExpectedContent()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains(DotNetJsFileName, runtime);
            Assert.Contains("dotnet.wasm", runtime);
            Assert.Contains("dotnet.timezones.dat", runtime);

            var assemblies = bootJsonData.resources.assembly.Keys;
            Assert.Contains("standalone.dll", assemblies);
            Assert.Contains("RazorClassLibrary.dll", assemblies);
            Assert.Contains("Microsoft.Extensions.Logging.Abstractions.dll", assemblies);

            Assert.Null(bootJsonData.resources.pdb);
            Assert.Null(bootJsonData.resources.satelliteResources);
        }

        [Fact]
        public async Task Build_WithBlazorEnableTimeZoneSupportDisabled_DoesNotCopyTimeZoneInfo()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
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
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp3.1";
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            var path = Path.GetFullPath(Path.Combine(project.SolutionPath, "standalone", "bin", project.Configuration, "netstandard2.1", "standalone.dll"));
            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorhosted.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("netstandard2.1", "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.Combine("razorclasslibrary", "wwwroot"));
            Assert.FileContains(result, staticWebAssets, Path.Combine("standalone", "wwwroot"));
        }

        [Fact]
        public async Task Build_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.AddProjectFileContent(
@"<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>false</BlazorWebAssemblyEnableLinking>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.timezones.dat");

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
        }

        [Fact]
        public async Task Build_SatelliteAssembliesAreCopiedToBuildOutput()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "classlibrarywithsatelliteassemblies" });
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

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "classlibrarywithsatelliteassemblies.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJsonPath = Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");
        }

        [Fact]
        public async Task Build_WithBlazorWebAssemblyEnableLinkingFalse_SatelliteAssembliesAreCopiedToBuildOutput()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary", "classlibrarywithsatelliteassemblies" });
            project.AddProjectFileContent(
@"
<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>false</BlazorWebAssemblyEnableLinking>
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

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "classlibrarywithsatelliteassemblies.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJson = ReadBootJsonData(result, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json"));

            var satelliteResources = bootJson.resources.satelliteResources;
            Assert.NotNull(satelliteResources);

            Assert.Contains("es-ES", satelliteResources.Keys);
            Assert.Contains("es-ES/classlibrarywithsatelliteassemblies.resources.dll", satelliteResources["es-ES"].Keys);
            Assert.Contains("fr", satelliteResources.Keys);
            Assert.Contains("fr/Microsoft.CodeAnalysis.CSharp.resources.dll", satelliteResources["fr"].Keys);
            Assert.Contains("ja", satelliteResources.Keys);
            Assert.Contains("ja/standalone.resources.dll", satelliteResources["ja"].Keys);
        }


        [Fact]
        public async Task Build_WithI8NOption_CopiesI8NAssembliesWithoutLinkerEnabled()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";
            project.AddProjectFileContent(
@"
<PropertyGroup>
    <BlazorWebAssemblyI18NAssemblies>other</BlazorWebAssemblyI18NAssemblies>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.Other.dll");
            // When running without linker, we copy all I18N assemblies. Look for one additional
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.West.dll");
        }

        [Fact]
        public async Task Build_WithI8NOption_CopiesI8NAssembliesWithLinkerEnabled()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            project.Configuration = "Debug";
            project.AddProjectFileContent(
@"
<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>true</BlazorWebAssemblyEnableLinking>
    <BlazorWebAssemblyI18NAssemblies>other</BlazorWebAssemblyI18NAssemblies>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.Other.dll");
            Assert.FileDoesNotExist(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "I18N.West.dll");
        }

        [Fact]
        public async Task Build_WithCustomOutputPath_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });

            project.AddDirectoryBuildContent(
@"<PropertyGroup>
    <BaseOutputPath>$(MSBuildThisFileDirectory)build\bin\</BaseOutputPath>
    <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)build\obj\</BaseIntermediateOutputPath>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/restore");
            Assert.BuildPassed(result);

            var compressedFilesPath = Path.Combine(
                project.DirectoryPath,
                "build",
                project.IntermediateOutputDirectory,
                "compressed");

            Assert.True(Directory.Exists(compressedFilesPath));
        }

        private static GenerateBlazorBootJson.BootJsonData ReadBootJsonData(MSBuildResult result, string path)
        {
            return JsonSerializer.Deserialize<GenerateBlazorBootJson.BootJsonData>(
                File.ReadAllText(Path.Combine(result.Project.DirectoryPath, path)),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
