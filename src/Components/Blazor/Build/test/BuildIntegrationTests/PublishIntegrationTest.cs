// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class PublishIntegrationTest
    {
        [Fact]
        public async Task Publish_WithDefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new [] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, Path.GetFileNameWithoutExtension(project.ProjectFilePath));

            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify referenced static web assets
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }

        [Fact]
        public async Task Publish_WithNoBuild_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:NoBuild=true");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, Path.GetFileNameWithoutExtension(project.ProjectFilePath));

            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify static web assets from referenced projects are copied.
            // Uncomment once https://github.com/dotnet/aspnetcore/issues/17426 is resolved.
            // Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            // Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }

        [Fact]
        public async Task Publish_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new [] { "razorclasslibrary" });
            project.AddProjectFileContent(
@"<PropertyGroup>
    <BlazorLinkOnBuild>false</BlazorLinkOnBuild>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, Path.GetFileNameWithoutExtension(project.ProjectFilePath));

            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify referenced static web assets
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_content", "RazorClassLibrary", "styles.css");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }

        [Fact]
        public async Task Publish_SatelliteAssemblies_AreCopiedToBuildOutput()
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

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/restore");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, Path.GetFileNameWithoutExtension(project.ProjectFilePath));

            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJsonPath = Path.Combine(blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");
        }

        [Fact]
        public async Task Publish_HostedApp_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp5.0";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "standalone");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify static web assets from referenced projects are copied.
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            var blazorConfig = Path.Combine(result.Project.DirectoryPath, publishDirectory, "standalone.blazor.config");
            var blazorConfigLines = File.ReadAllLines(blazorConfig);
            Assert.Equal(".", blazorConfigLines[0]);
            Assert.Equal("standalone/", blazorConfigLines[1]);
        }

        [Fact]
        public async Task Publish_HostedApp_WithNoBuild_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp5.0";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:NoBuild=true");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "standalone");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "dotnet.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify static web assets from referenced projects are copied.
            // Uncomment once https://github.com/dotnet/aspnetcore/issues/17426 is resolved.
            // Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            // Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }
    }
}
