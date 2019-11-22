// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class BuildIntegrationTest
    {
        [Fact]
        public async Task Build_WithDefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
        }

        [Fact]
        public async Task Build_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");
            project.AddProjectFileContent(
@"<PropertyGroup>
    <BlazorLinkOnBuild>false</BlazorLinkOnBuild>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.wasm");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "wasm", "mono.js");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "dist", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
        }
    }
}
