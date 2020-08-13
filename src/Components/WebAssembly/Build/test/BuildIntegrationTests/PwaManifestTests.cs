using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class PwaManifestTests
    {
        [Fact]
        public async Task Build_ServiceWorkerAssetsManifest_Works()
        {
            // Arrange
            var expectedExtensions = new[] { ".dll", ".pdb", ".js", ".wasm" };
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileCountEquals(result, 1, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "wasm"), "dotnet.*.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "standalone.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("netstandard2.1", "wwwroot"));

            var serviceWorkerAssetsManifest = Assert.FileExists(result, buildOutputDirectory, "wwwroot", "service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest).TrimEnd()[22..^1];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.All(entries, e => expectedExtensions.Contains(Path.GetExtension(e)));
        }

        [Fact]
        public async Task Publish_UpdatesServiceWorkerVersionHash_WhenSourcesChange()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
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
            var updatedResult = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:updated.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

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
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new[] { "razorclasslibrary" });
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

            // Act && Assert
            var updatedResult = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", args: "/bl:updated.binlog /p:ServiceWorkerAssetsManifest=service-worker-assets.js");

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
