// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Build;
using Xunit;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;
using static Microsoft.AspNetCore.Components.WebAssembly.Build.WebAssemblyRuntimePackage;
using System.IO.Compression;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class PublishIntegrationTest
    {
        [Fact]
        public async Task Publish_WithDefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new [] { "razorclasslibrary", "LinkBaseToWebRoot" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify referenced static web assets
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify link item assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "js", "LinkedScript.js");
            var cssFile = Assert.FileExists(result, blazorPublishDirectory, "css", "app.css");
            Assert.FileContains(result, cssFile, ".publish");
            Assert.FileDoesNotExist(result, "dist", "Fake-License.txt");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
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
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify static web assets from referenced projects are copied.
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");

            VerifyCompression(result, blazorPublishDirectory);
        }

        private static void VerifyCompression(MSBuildResult result, string blazorPublishDirectory)
        {
            var original = Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            var compressed = Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json.br");
            using var brotliStream = new BrotliStream(File.OpenRead(compressed), CompressionMode.Decompress);
            using var textReader = new StreamReader(brotliStream);
            var uncompressedText = textReader.ReadToEnd();
            var originalText = File.ReadAllText(original);

            Assert.Equal(originalText, uncompressedText);
        }

        [Fact]
        public async Task Publish_WithLinkOnBuildDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone", additionalProjects: new [] { "razorclasslibrary" });
            project.AddProjectFileContent(
@"<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>false</BlazorWebAssemblyEnableLinking>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify referenced static web assets
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, blazorPublishDirectory, "_content", "RazorClassLibrary", "styles.css");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
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
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJsonPath = Path.Combine(blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        [Fact]
        public async Task Publish_HostedApp_WithLinkOnBuildTrue_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp3.1";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");
            AddSiblingProjectFileContent(project, @"
<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>true</BlazorWebAssemblyEnableLinking>
</PropertyGroup>");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");
            Assert.FileExists(result, publishDirectory, "RazorClassLibrary.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.
            // Verify project references appear as static web assets
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "RazorClassLibrary.dll");
            // Also verify project references to the server project appear in the publish output
            Assert.FileExists(result, publishDirectory, "RazorClassLibrary.dll");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify static web assets from referenced projects are copied.
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        [Fact]
        // Regression test for https://github.com/dotnet/aspnetcore/issues/18752
        public async Task Publish_HostedApp_WithLinkOnBuildFalse_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp3.1";

            AddSiblingProjectFileContent(project, @"
<PropertyGroup>
    <BlazorWebAssemblyEnableLinking>false</BlazorWebAssemblyEnableLinking>
</PropertyGroup>");

            // VS builds projects individually and then a publish with BuildDependencies=false, but building the main project is a close enough approximation for this test.
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            // Publish
            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:BuildDependencies=false");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            // Verification for https://github.com/dotnet/aspnetcore/issues/19926. Verify binaries for projects
            // referenced by the Hosted project appear in the publish directory
            Assert.FileExists(result, publishDirectory, "RazorClassLibrary.dll");
            Assert.FileExists(result, publishDirectory, "standalone.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "RazorClassLibrary.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify static web assets from referenced projects are copied.
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");

            }

        [Fact]
        public async Task Publish_HostedApp_WithNoBuild_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "standalone", "razorclasslibrary", });
            project.TargetFramework = "netcoreapp3.1";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:NoBuild=true");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "wasm", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify static web assets from referenced projects are copied.
            // Uncomment once https://github.com/aspnet/AspNetCore/issues/17426 is resolved.
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        private static void AddSiblingProjectFileContent(ProjectDirectory project, string content)
        {
            var path = Path.Combine(project.SolutionPath, "standalone", "standalone.csproj");
            var existing = File.ReadAllText(path);
            var updated = existing.Replace("<!-- Test Placeholder -->", content);
            File.WriteAllText(path, updated);
        }

        private static void VerifyBootManifestHashes(MSBuildResult result, string blazorPublishDirectory)
        {
            var bootManifestResolvedPath = Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            var bootManifestJson = File.ReadAllText(bootManifestResolvedPath);
            var bootManifest = JsonSerializer.Deserialize<GenerateBlazorBootJson.BootJsonData>(bootManifestJson);

            VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.assembly, r => $"_framework/_bin/{r}");
            VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.runtime, r => $"_framework/wasm/{r}");

            if (bootManifest.resources.pdb != null)
            {
                VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.pdb, r => $"_framework/_bin/{r}");
            }

            if (bootManifest.resources.satelliteResources != null)
            {
                foreach (var resourcesForCulture in bootManifest.resources.satelliteResources.Values)
                {
                    VerifyBootManifestHashes(result, blazorPublishDirectory, resourcesForCulture, r => $"_framework/_bin/{r}");
                }
            }
        }

        private static void VerifyBootManifestHashes(MSBuildResult result, string blazorPublishDirectory, ResourceHashesByNameDictionary resources, Func<string, string> relativePathFunc)
        {
            foreach (var (name, hash) in resources)
            {
                var relativePath = Path.Combine(blazorPublishDirectory, relativePathFunc(name));
                Assert.FileHashEquals(result, relativePath, ParseWebFormattedHash(hash));
            }
        }

        private static void VerifyServiceWorkerFiles(MSBuildResult result, string blazorPublishDirectory, string serviceWorkerPath, string serviceWorkerContent, string assetsManifestPath)
        {
            // Check the expected files are there
            var serviceWorkerResolvedPath = Assert.FileExists(result, blazorPublishDirectory, serviceWorkerPath);
            var assetsManifestResolvedPath = Assert.FileExists(result, blazorPublishDirectory, assetsManifestPath);

            // Check the service worker contains the expected content (which comes from the PublishedContent file)
            Assert.FileContains(result, serviceWorkerResolvedPath, serviceWorkerContent);

            // Check the assets manifest version was added to the published service worker
            var assetsManifest = ReadServiceWorkerAssetsManifest(assetsManifestResolvedPath);
            Assert.FileContains(result, serviceWorkerResolvedPath, $"/* Manifest version: {assetsManifest.version} */");

            // Check the assets manifest contains correct entries for all static content we're publishing
            var resolvedPublishDirectory = Path.Combine(result.Project.DirectoryPath, blazorPublishDirectory);
            var publishedStaticFiles = Directory.GetFiles(resolvedPublishDirectory, "*", new EnumerationOptions { RecurseSubdirectories = true });
            var assetsManifestHashesByUrl = (IReadOnlyDictionary<string, string>)assetsManifest.assets.ToDictionary(x => x.url, x => x.hash);
            foreach (var publishedFilePath in publishedStaticFiles)
            {
                var publishedFileRelativePath = Path.GetRelativePath(resolvedPublishDirectory, publishedFilePath);

                // We don't list compressed files in the SWAM, as these are transparent to the client,
                // nor do we list the service worker itself or its assets manifest, as these don't need to be fetched in the same way
                if (IsCompressedFile(publishedFileRelativePath)
                    || string.Equals(publishedFileRelativePath, serviceWorkerPath, StringComparison.Ordinal)
                    || string.Equals(publishedFileRelativePath, assetsManifestPath, StringComparison.Ordinal))
                {
                    continue;
                }

                // Verify hash
                var publishedFileUrl = publishedFileRelativePath.Replace('\\', '/');
                var expectedHash = ParseWebFormattedHash(assetsManifestHashesByUrl[publishedFileUrl]);
                Assert.Contains(publishedFileUrl, assetsManifestHashesByUrl);
                Assert.FileHashEquals(result, publishedFilePath, expectedHash);
            }
        }

        private static string ParseWebFormattedHash(string webFormattedHash)
        {
            Assert.StartsWith("sha256-", webFormattedHash);
            return webFormattedHash.Substring(7);
        }

        private static bool IsCompressedFile(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".br":
                case ".gz":
                    return true;
                default:
                    return false;
            }
        }

        private static GenerateServiceWorkerAssetsManifest.AssetsManifestFile ReadServiceWorkerAssetsManifest(string assetsManifestResolvedPath)
        {
            var jsContents = File.ReadAllText(assetsManifestResolvedPath);
            var jsonStart = jsContents.IndexOf("{");
            var jsonLength = jsContents.LastIndexOf("}") - jsonStart + 1;
            var json = jsContents.Substring(jsonStart, jsonLength);
            return JsonSerializer.Deserialize<GenerateServiceWorkerAssetsManifest.AssetsManifestFile>(json);
        }
    }
}
