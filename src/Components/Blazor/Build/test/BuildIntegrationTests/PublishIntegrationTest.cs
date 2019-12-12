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
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.js");
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
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify static web assets from referenced projects are copied.
            // Uncomment once https://github.com/aspnet/AspNetCore/issues/17426 is resolved.
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
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.js");
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
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.js");
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
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify static web assets from referenced projects are copied.
            // Uncomment once https://github.com/aspnet/AspNetCore/issues/17426 is resolved.
            // Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            // Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "dist", "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }
    }
}
