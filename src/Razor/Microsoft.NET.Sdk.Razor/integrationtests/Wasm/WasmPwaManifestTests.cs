// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.AspNetCore.Razor.Design.IntegrationTests.ServiceWorkerAssert;


namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class WasmPwaManifestTests
    {
        private static readonly string DotNetJsFileName = "dotnet.js";

        [Fact]
        public async Task Build_ServiceWorkerAssetsManifest_Works()
        {
            // Arrange
            var expectedExtensions = new[] { ".dll", ".pdb", ".js", ".wasm" };
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "dotnet.wasm");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", DotNetJsFileName);
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorwasm.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine(project.TargetFramework, "wwwroot"));

            var serviceWorkerAssetsManifest = Assert.FileExists(result, buildOutputDirectory, "wwwroot", "service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest).TrimEnd()[22..^1];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.All(entries, e => expectedExtensions.Contains(Path.GetExtension(e)));

            VerifyServiceWorkerFiles(result, 
               Path.Combine(buildOutputDirectory, "wwwroot"),
               serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
               serviceWorkerContent: "// This is the development service worker",
               assetsManifestPath: "service-worker-assets.js");
        }

        [Fact]
        public async Task Build_HostedAppWithServiceWorker_Works()
        {
            // Arrange
            var expectedExtensions = new[] { ".dll", ".pdb", ".js", ".wasm" };
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project);

            Assert.BuildPassed(result);

            var wasmProject = project.GetSibling("blazorwasm");
            var buildOutputDirectory = Path.Combine(wasmProject.DirectoryPath, wasmProject.BuildOutputDirectory);

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "blazorwasm.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine(project.TargetFramework, "wwwroot"));

            var serviceWorkerAssetsManifest = Assert.FileExists(result, buildOutputDirectory, "wwwroot", "custom-service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest).TrimEnd()[22..^1];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.All(entries, e => expectedExtensions.Contains(Path.GetExtension(e)));
        }

        [Fact]
        public async Task PublishWithPWA_ProducesAssets()
        {
            // Arrange
            var expectedExtensions = new[] { ".dll", ".pdb", ".js", ".wasm" };
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "Publish");

            Assert.BuildPassed(result);

            var publishOutputDirectory = project.PublishOutputDirectory;

            var serviceWorkerAssetsManifest = Assert.FileExists(result, publishOutputDirectory, "wwwroot", "custom-service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest).TrimEnd()[22..^1];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.All(entries, e => expectedExtensions.Contains(Path.GetExtension(e)));

            var serviceWorkerFile = Path.Combine(publishOutputDirectory, "wwwroot", "serviceworkers", "my-service-worker.js");
            Assert.FileContainsLine(result, serviceWorkerFile, "// This is the production service worker");
        }

        [Fact]
        public async Task PublishHostedWithPWA_ProducesAssets()
        {
            // Arrange
            var expectedExtensions = new[] { ".dll", ".pdb", ".js", ".wasm" };
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, target: "Publish");

            Assert.BuildPassed(result);

            var publishOutputDirectory = project.PublishOutputDirectory;

            var serviceWorkerAssetsManifest = Assert.FileExists(result, publishOutputDirectory, "wwwroot", "custom-service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest).TrimEnd()[22..^1];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.All(entries, e => expectedExtensions.Contains(Path.GetExtension(e)));

            var serviceWorkerFile = Path.Combine(publishOutputDirectory, "wwwroot", "serviceworkers", "my-service-worker.js");
            Assert.FileContainsLine(result, serviceWorkerFile, "// This is the production service worker");
        }

        [Fact]
        public async Task Publish_UpdatesServiceWorkerVersionHash_WhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:initial.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var publishOutputDirectory = project.PublishOutputDirectory;

            var serviceWorkerFile = Assert.FileExists(result, publishOutputDirectory, "wwwroot", "serviceworkers", "my-service-worker.js");
            var version = File.ReadAllLines(serviceWorkerFile).Last();
            var match = Regex.Match(version, "\\/\\* Manifest version: (.{8}) \\*\\/");
            Assert.True(match.Success);
            Assert.Equal(2, match.Groups.Count);
            Assert.NotNull(match.Groups[1].Value);
            var capture = match.Groups[1].Value;

            // Act
            var cssFile = Path.Combine(project.DirectoryPath, "LinkToWebRoot", "css", "app.css");
            File.WriteAllText(cssFile, ".updated { }");

            // Assert
            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:updated.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var updatedVersion = File.ReadAllLines(serviceWorkerFile).Last();
            var updatedMatch = Regex.Match(updatedVersion, "\\/\\* Manifest version: (.{8}) \\*\\/");
            Assert.True(updatedMatch.Success);
            Assert.Equal(2, updatedMatch.Groups.Count);
            Assert.NotNull(updatedMatch.Groups[1].Value);
            var updatedCapture = updatedMatch.Groups[1].Value;

            Assert.NotEqual(capture, updatedCapture);
        }

        [Fact]
        public async Task Publish_DeterministicAcrossBuilds_WhenNoSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:initial.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var publishOutputDirectory = project.PublishOutputDirectory;

            var serviceWorkerFile = Assert.FileExists(result, publishOutputDirectory, "wwwroot", "serviceworkers", "my-service-worker.js");
            var version = File.ReadAllLines(serviceWorkerFile).Last();
            var match = Regex.Match(version, "\\/\\* Manifest version: (.{8}) \\*\\/");
            Assert.True(match.Success, $"No match found for manifest version regex in line: {version}");
            Assert.Equal(2, match.Groups.Count);
            Assert.NotNull(match.Groups[1].Value);
            var capture = match.Groups[1].Value;

            // Act && Assert
            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:updated.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var updatedVersion = File.ReadAllLines(serviceWorkerFile).Last();
            var updatedMatch = Regex.Match(updatedVersion, "\\/\\* Manifest version: (.{8}) \\*\\/");
            Assert.True(updatedMatch.Success);
            Assert.Equal(2, updatedMatch.Groups.Count);
            Assert.NotNull(updatedMatch.Groups[1].Value);
            var updatedCapture = updatedMatch.Groups[1].Value;

            Assert.Equal(capture, updatedCapture);
        }
    }
}
