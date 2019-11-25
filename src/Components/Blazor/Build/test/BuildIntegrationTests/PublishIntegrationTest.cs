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
            using var project = ProjectDirectory.Create("standalone");
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

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }

        [Fact]
        public async Task Publish_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
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

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
        }
    }
}
