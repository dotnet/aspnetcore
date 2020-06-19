// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;
using static Microsoft.AspNetCore.Razor.Design.IntegrationTests.ServiceWorkerAssert;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class WasmPublishIntegrationTest
    {
        private static readonly string DotNetJsFileName = "dotnet.js";

        [Fact]
        public async Task Publish_WithDefaultSettings_Works()
        {
            // Arrange
            var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary", "LinkBaseToWebRoot" });
            project.Configuration = "Debug";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

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
            Assert.FileCountEquals(result, 1, publishDirectory, "*", SearchOption.TopDirectoryOnly);

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        [Fact]
        public async Task Publish_InRelease_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary", "LinkBaseToWebRoot" });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "");

            Assert.BuildPassed(result);


            var publishDirectory = project.PublishOutputDirectory;

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

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
            Assert.FileCountEquals(result, 1, publishDirectory, "*", SearchOption.TopDirectoryOnly);
        }

        [Fact]
        public async Task Publish_WithExistingWebConfig_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary", "LinkBaseToWebRoot" });
            project.Configuration = "Release";

            var webConfigContents = "test webconfig contents";
            AddFileToProject(project, "web.config", webConfigContents);

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");
            Assert.FileContains(result, Path.Combine(publishDirectory, "web.config"), webConfigContents);
        }

        [Fact]
        public async Task Publish_WithNoBuild_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:NoBuild=true");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

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
        public async Task Publish_WithTrimmingdDisabled_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary" });
            project.AddProjectFileContent(
@"<PropertyGroup>
    <PublishTrimmed>false</PublishTrimmed>
</PropertyGroup>");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");

            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.
            
            // Verify compression works
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll.br"); // 

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
            using var project = ProjectDirectory.Create("blazorwasm", additionalProjects: new[] { "razorclasslibrary", "classlibrarywithsatelliteassemblies" });
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

            Assert.FileExists(result, blazorPublishDirectory, "_framework", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            var bootJsonPath = Path.Combine(blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
        }

        [Fact]
        [QuarantinedTest]
        public async Task Publish_HostedApp_DefaultSettings_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");
            Assert.BuildPassed(result);

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");
            
            // Verification for https://github.com/dotnet/aspnetcore/issues/19926. Verify binaries for projects
            // referenced by the Hosted project appear in the publish directory
            Assert.FileExists(result, publishDirectory, "RazorClassLibrary.dll");
            Assert.FileExists(result, publishDirectory, "blazorwasm.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.
                                                                                                                                      
            // Verify project references appear as static web assets
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "RazorClassLibrary.dll");
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

            // Verify compression works
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "RazorClassLibrary.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll.br");

            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        [Fact]
        public async Task Publish_HostedApp_ProducesBootJsonDataWithExpectedContent()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            var wwwroot = Path.Combine(project.DirectoryPath, "..", "blazorwasm", "wwwroot");
            File.WriteAllText(Path.Combine(wwwroot, "appsettings.json"), "Default settings");
            File.WriteAllText(Path.Combine(wwwroot, "appsettings.development.json"), "Development settings");

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish");
            Assert.BuildPassed(result);

            var bootJsonPath = Path.Combine(project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var runtime = bootJsonData.resources.runtime.Keys;
            Assert.Contains(DotNetJsFileName, runtime);
            Assert.Contains("dotnet.wasm", runtime);

            var assemblies = bootJsonData.resources.assembly.Keys;
            Assert.Contains("blazorwasm.dll", assemblies);
            Assert.Contains("RazorClassLibrary.dll", assemblies);
            Assert.Contains("System.Text.Json.dll", assemblies);

            // No pdbs
            Assert.Null(bootJsonData.resources.pdb);
            Assert.Null(bootJsonData.resources.satelliteResources);

            Assert.Contains("appsettings.json", bootJsonData.config);
            Assert.Contains("appsettings.development.json", bootJsonData.config);
        }

        [Fact]
        public async Task Publish_HostedApp_WithSatelliteAssemblies()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", "classlibrarywithsatelliteassemblies", });
            var wasmProject = project.GetSibling("blazorwasm");

            wasmProject.AddProjectFileContent(
@"
<PropertyGroup>
    <DefineConstants>$(DefineConstants);REFERENCE_classlibrarywithsatelliteassemblies</DefineConstants>
</PropertyGroup>
<ItemGroup>
    <ProjectReference Include=""..\classlibrarywithsatelliteassemblies\classlibrarywithsatelliteassemblies.csproj"" />
</ItemGroup>");
            var resxfileInProject = Path.Combine(wasmProject.DirectoryPath, "Resources.ja.resx.txt");
            File.Move(resxfileInProject, Path.Combine(wasmProject.DirectoryPath, "Resource.ja.resx"));

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/restore");
            Assert.BuildPassed(result);

            var bootJsonPath = Path.Combine(project.PublishOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            var bootJsonData = ReadBootJsonData(result, bootJsonPath);

            var publishOutputDirectory = project.PublishOutputDirectory;

            Assert.FileExists(result, publishOutputDirectory, "wwwroot", "_framework", "blazorwasm.dll");
            Assert.FileExists(result, publishOutputDirectory, "wwwroot", "_framework", "classlibrarywithsatelliteassemblies.dll");
            Assert.FileExists(result, publishOutputDirectory, "wwwroot", "_framework", "Microsoft.CodeAnalysis.CSharp.dll");
            Assert.FileExists(result, publishOutputDirectory, "wwwroot", "_framework", "fr", "Microsoft.CodeAnalysis.CSharp.resources.dll"); // Verify satellite assemblies are present in the build output.

            Assert.FileContains(result, bootJsonPath, "\"Microsoft.CodeAnalysis.CSharp.dll\"");
            Assert.FileContains(result, bootJsonPath, "\"fr\\/Microsoft.CodeAnalysis.CSharp.resources.dll\"");
        }

        [Fact]
        // Regression test for https://github.com/dotnet/aspnetcore/issues/18752
        public async Task Publish_HostedApp_WithoutTrimming_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            AddWasmProjectContent(project, @"
        <PropertyGroup>
            <PublishTrimmed>false</PublishTrimmed>
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
            Assert.FileExists(result, publishDirectory, "blazorwasm.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

            // Verify project references appear as static web assets
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "RazorClassLibrary.dll");
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

            // Verify compression works
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "RazorClassLibrary.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll.br");

            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        [Fact]
        public async Task Publish_HostedApp_WithNoBuild_Works()
        {
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:NoBuild=true");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

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
        public async Task Publish_HostedApp_VisualStudio()
        {
            // Simulates publishing the same way VS does by setting BuildProjectReferences=false.
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", });
            project.Configuration = "Release";
            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build", "/p:BuildInsideVisualStudio=true");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:BuildProjectReferences=false /p:BuildInsideVisualStudio=true");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", DotNetJsFileName);
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll"); // Verify dependencies are part of the output.

            // Verify static assets are in the publish directory
            Assert.FileExists(result, blazorPublishDirectory, "index.html");

            // Verify static web assets from referenced projects are copied.
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "wwwroot", "exampleJsInterop.js");
            Assert.FileExists(result, publishDirectory, "wwwroot", "_content", "RazorClassLibrary", "styles.css");

            // Verify compression works
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "dotnet.wasm.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazorwasm.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "RazorClassLibrary.dll.br");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "System.Text.Json.dll.br");

            // Verify web.config
            Assert.FileExists(result, publishDirectory, "web.config");

            VerifyBootManifestHashes(result, blazorPublishDirectory);
            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        // Regression test to verify satellite assemblies from the blazor app are copied to the published app's wwwroot output directory as
        // part of publishing in VS
        [Fact]
        public async Task Publish_HostedApp_VisualStudio_WithSatelliteAssemblies()
        {
            // Simulates publishing the same way VS does by setting BuildProjectReferences=false.
            // Arrange
            using var project = ProjectDirectory.Create("blazorhosted", additionalProjects: new[] { "blazorwasm", "razorclasslibrary", "classlibrarywithsatelliteassemblies" });
            var blazorwasmProjectDirectory = Path.Combine(project.DirectoryPath, "..", "blazorwasm");

            // Setup the blazorwasm app to have it's own satellite assemblies as well as transitively imported satellite assemblies.
            var resxfileInProject = Path.Combine(blazorwasmProjectDirectory, "Resources.ja.resx.txt");
            File.Move(resxfileInProject, Path.Combine(blazorwasmProjectDirectory, "Resource.ja.resx"));

            var blazorwasmProjFile = Path.Combine(blazorwasmProjectDirectory, "blazorwasm.csproj");
            var existing = File.ReadAllText(blazorwasmProjFile);
            var updatedContent = @"
        <PropertyGroup>
            <DefineConstants>$(DefineConstants);REFERENCE_classlibrarywithsatelliteassemblies</DefineConstants>
        </PropertyGroup>
        <ItemGroup>
            <ProjectReference Include=""..\classlibrarywithsatelliteassemblies\classlibrarywithsatelliteassemblies.csproj"" />
        </ItemGroup>";
            var updated = existing.Replace("<!-- Test Placeholder -->", updatedContent);
            File.WriteAllText(blazorwasmProjFile, updated);

            var result = await MSBuildProcessManager.DotnetMSBuild(project, "Build", $"/restore");

            Assert.BuildPassed(result);

            result = await MSBuildProcessManager.DotnetMSBuild(project, "Publish", "/p:BuildProjectReferences=false");

            var publishDirectory = project.PublishOutputDirectory;
            // Make sure the main project exists
            Assert.FileExists(result, publishDirectory, "blazorhosted.dll");

            var blazorPublishDirectory = Path.Combine(publishDirectory, "wwwroot");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "ja", "blazorwasm.resources.dll");
            Assert.FileExists(result, blazorPublishDirectory, "_framework", "fr", "Microsoft.CodeAnalysis.resources.dll");

            var bootJson = Path.Combine(project.DirectoryPath, blazorPublishDirectory, "_framework", "blazor.boot.json");
            var bootJsonFile = JsonSerializer.Deserialize<BootJsonData>(File.ReadAllText(bootJson), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var satelliteResources = bootJsonFile.resources.satelliteResources;

            Assert.Contains("es-ES", satelliteResources.Keys);
            Assert.Contains("es-ES/classlibrarywithsatelliteassemblies.resources.dll", satelliteResources["es-ES"].Keys);
            Assert.Contains("fr", satelliteResources.Keys);
            Assert.Contains("fr/Microsoft.CodeAnalysis.CSharp.resources.dll", satelliteResources["fr"].Keys);
            Assert.Contains("ja", satelliteResources.Keys);
            Assert.Contains("ja/blazorwasm.resources.dll", satelliteResources["ja"].Keys);

            VerifyServiceWorkerFiles(result, blazorPublishDirectory,
                serviceWorkerPath: Path.Combine("serviceworkers", "my-service-worker.js"),
                serviceWorkerContent: "// This is the production service worker",
                assetsManifestPath: "custom-service-worker-assets.js");
        }

        private static void AddWasmProjectContent(ProjectDirectory project, string content)
        {
            var path = Path.Combine(project.SolutionPath, "blazorwasm", "blazorwasm.csproj");
            var existing = File.ReadAllText(path);
            var updated = existing.Replace("<!-- Test Placeholder -->", content);
            File.WriteAllText(path, updated);
        }

        private static void AddFileToProject(ProjectDirectory project, string filename, string content)
        {
            var path = Path.Combine(project.DirectoryPath, filename);
            File.WriteAllText(path, content);
        }

        private static void VerifyBootManifestHashes(MSBuildResult result, string blazorPublishDirectory)
        {
            var bootManifestResolvedPath = Assert.FileExists(result, blazorPublishDirectory, "_framework", "blazor.boot.json");
            var bootManifestJson = File.ReadAllText(bootManifestResolvedPath);
            var bootManifest = JsonSerializer.Deserialize<BootJsonData>(bootManifestJson);

            VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.assembly);
            VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.runtime);

            if (bootManifest.resources.pdb != null)
            {
                VerifyBootManifestHashes(result, blazorPublishDirectory, bootManifest.resources.pdb);
            }

            if (bootManifest.resources.satelliteResources != null)
            {
                foreach (var resourcesForCulture in bootManifest.resources.satelliteResources.Values)
                {
                    VerifyBootManifestHashes(result, blazorPublishDirectory, resourcesForCulture);
                }
            }

            static void VerifyBootManifestHashes(MSBuildResult result, string blazorPublishDirectory, ResourceHashesByNameDictionary resources)
            {
                foreach (var (name, hash) in resources)
                {
                    var relativePath = Path.Combine(blazorPublishDirectory, "_framework", name);
                    Assert.FileHashEquals(result, relativePath, ParseWebFormattedHash(hash));
                }
            }

            static string ParseWebFormattedHash(string webFormattedHash)
            {
                Assert.StartsWith("sha256-", webFormattedHash);
                return webFormattedHash.Substring(7);
            }
        }

        private static BootJsonData ReadBootJsonData(MSBuildResult result, string path)
        {
            return JsonSerializer.Deserialize<BootJsonData>(
                File.ReadAllText(Path.Combine(result.Project.DirectoryPath, path)),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
