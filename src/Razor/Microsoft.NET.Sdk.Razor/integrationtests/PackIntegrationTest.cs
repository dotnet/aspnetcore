// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class PackIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        private static readonly string TFM = "net6.0";

        public PackIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_NoBuild_IncludeRazorContent_IncludesRazorViewContent()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            result = await DotnetMSBuild("Pack", "/p:NoBuild=true /p:IncludeRazorContentInPack=true");
            Assert.BuildPassed(result);

            Assert.NuspecContains(
                result,
                Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                $@"<files include=""any/{TFM}/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("contentFiles", "any", TFM, "Views", "Shared", "_Layout.cshtml"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_NoBuild_Works_IncludesRazorAssembly()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            result = await DotnetMSBuild("Pack", "/p:NoBuild=true");
            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecDoesNotContain(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.pdb")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.pdb")}\" />");
            }

            Assert.NuspecDoesNotContain(
                result,
                Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                $@"<files include=""any/{TFM}/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", TFM, "ClassLibrary.Views.dll"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_Works_IncludesRazorAssembly()
        {
            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecDoesNotContain(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.pdb")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.pdb")}\" />");
            }

            Assert.NuspecDoesNotContain(
                result,
                Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                $@"<files include=""any/{TFM}/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", TFM, "ClassLibrary.Views.dll"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_WithIncludeSymbols_IncludesRazorPdb()
        {
            var result = await DotnetMSBuild("Pack", "/p:RazorCompileOnBuild=true /p:IncludeSymbols=true");

            Assert.BuildPassed(result);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.symbols.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.symbols.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.pdb")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.pdb")}\" />");
            }

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.symbols.nupkg"),
                Path.Combine("lib", TFM, "ClassLibrary.Views.dll"),
                Path.Combine("lib", TFM, "ClassLibrary.Views.pdb"));
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Pack_IncludesRazorFilesAsContent_WhenIncludeRazorContentInPack_IsSet()
        {
            var result = await DotnetMSBuild("Pack", "/p:IncludeRazorContentInPack=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Travis on OSX produces different full paths in C# and MSBuild
                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $"<file src=\"{Path.Combine(Project.DirectoryPath, "bin", Configuration, TFM, "ClassLibrary.Views.dll")}\" " +
                    $"target=\"{Path.Combine("lib", TFM, "ClassLibrary.Views.dll")}\" />");

                Assert.NuspecContains(
                    result,
                    Path.Combine("obj", Configuration, "ClassLibrary.1.0.0.nuspec"),
                    $@"<files include=""any/{TFM}/Views/Shared/_Layout.cshtml"" buildAction=""Content"" />");
            }

            Assert.NupkgContains(
                result,
                Path.Combine("bin", Configuration, "ClassLibrary.1.0.0.nupkg"),
                Path.Combine("lib", TFM, "ClassLibrary.Views.dll"));
        }

        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_FailsWhenStaticWebAssetsHaveConflictingPaths()
        {
            Project.AddProjectFileContent(@"
<ItemGroup>
  <StaticWebAsset Include=""bundle\js\pkg-direct-dep.js"">
    <SourceType></SourceType>
    <SourceId>PackageLibraryDirectDependency</SourceId>
    <ContentRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)\bundle\'))</ContentRoot>
    <BasePath>_content/PackageLibraryDirectDependency</BasePath>
    <RelativePath>js/pkg-direct-dep.js</RelativePath>
  </StaticWebAsset>
</ItemGroup>");

            Directory.CreateDirectory(Path.Combine(Project.DirectoryPath, "bundle", "js"));
            File.WriteAllText(Path.Combine(Project.DirectoryPath, "bundle", "js", "pkg-direct-dep.js"), "console.log('bundle');");

            var result = await DotnetMSBuild("Pack");

            Assert.BuildFailed(result);
        }

        // If you modify this test, make sure you also modify the test below this one to assert that things are not included as content.
        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_IncludesStaticWebAssets()
        {
            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "PackageLibraryDirectDependency.dll");

            Assert.NupkgContains(
                result,
                Path.Combine("..", "TestPackageRestoreSource", "PackageLibraryDirectDependency.1.0.0.nupkg"),
                filePaths: new[]
                {
                    Path.Combine("staticwebassets", "js", "pkg-direct-dep.js"),
                    Path.Combine("staticwebassets", "css", "site.css"),
                    Path.Combine("staticwebassets", "PackageLibraryDirectDependency.bundle.scp.css"),
                    Path.Combine("build", "Microsoft.AspNetCore.StaticWebAssets.props"),
                    Path.Combine("build", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildMultiTargeting", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildTransitive", "PackageLibraryDirectDependency.props")
                });
        }

        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_DoesNotInclude_TransitiveBundleOrScopedCssAsStaticWebAsset()
        {
            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "PackageLibraryDirectDependency.dll");

            Assert.NupkgDoesNotContain(
                result,
                Path.Combine("..", "TestPackageRestoreSource", "PackageLibraryDirectDependency.1.0.0.nupkg"),
                filePaths: new[]
                {
                    // This is to make sure we don't include the scoped css files on the package when bundling is enabled.
                    Path.Combine("staticwebassets", "Components", "App.razor.rz.scp.css"),
                    Path.Combine("staticwebassets", "PackageLibraryDirectDependency.styles.css"),
                });
        }

        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_DoesNotIncludeStaticWebAssetsAsContent()
        {
            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "PackageLibraryDirectDependency.dll");

            Assert.NupkgDoesNotContain(
                result,
                Path.Combine("..", "TestPackageRestoreSource", "PackageLibraryDirectDependency.1.0.0.nupkg"),
                filePaths: new[]
                {
                    Path.Combine("content", "js", "pkg-direct-dep.js"),
                    Path.Combine("content", "css", "site.css"),
                    Path.Combine("content", "Components", "App.razor.css"),
                    // This is to make sure we don't include the unscoped css file on the package.
                    Path.Combine("content", "Components", "App.razor.css"),
                    Path.Combine("content", "Components", "App.razor.rz.scp.css"),
                    Path.Combine("contentFiles", "js", "pkg-direct-dep.js"),
                    Path.Combine("contentFiles", "css", "site.css"),
                    Path.Combine("contentFiles", "Components", "App.razor.css"),
                    Path.Combine("contentFiles", "Components", "App.razor.rz.scp.css"),
                });
        }

        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_StaticWebAssetsEnabledFalse_DoesNotPackAnyStaticWebAssets()
        {
            var result = await DotnetMSBuild("Pack", "/p:StaticWebAssetsEnabled=false");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "PackageLibraryDirectDependency.dll");

            Assert.NupkgDoesNotContain(
                result,
                Path.Combine("..", "TestPackageRestoreSource", "PackageLibraryDirectDependency.1.0.0.nupkg"),
                filePaths: new[]
                {
                    Path.Combine("staticwebassets", "js", "pkg-direct-dep.js"),
                    Path.Combine("staticwebassets", "css", "site.css"),
                    Path.Combine("build", "Microsoft.AspNetCore.StaticWebAssets.props"),
                    Path.Combine("build", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildMultiTargeting", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildTransitive", "PackageLibraryDirectDependency.props")
                });
        }

        [Fact]
        [InitializeTestProject("PackageLibraryDirectDependency", additionalProjects: new[] { "PackageLibraryTransitiveDependency" })]
        public async Task Pack_NoBuild_IncludesStaticWebAssets()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result, allowWarnings: true);

            var pack = await DotnetMSBuild("Pack", "/p:NoBuild=true");
            Assert.BuildPassed(pack, allowWarnings: true);

            Assert.FileExists(pack, OutputPath, "PackageLibraryDirectDependency.dll");

            Assert.NupkgContains(
                pack,
                Path.Combine("..", "TestPackageRestoreSource", "PackageLibraryDirectDependency.1.0.0.nupkg"),
                filePaths: new[]
                {
                    Path.Combine("staticwebassets", "js", "pkg-direct-dep.js"),
                    Path.Combine("staticwebassets", "PackageLibraryDirectDependency.bundle.scp.css"),
                    Path.Combine("staticwebassets", "css", "site.css"),
                    Path.Combine("build", "Microsoft.AspNetCore.StaticWebAssets.props"),
                    Path.Combine("build", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildMultiTargeting", "PackageLibraryDirectDependency.props"),
                    Path.Combine("buildTransitive", "PackageLibraryDirectDependency.props")
                });
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task Pack_DoesNotIncludeAnyCustomPropsFiles_WhenNoStaticAssetsAreAvailable()
        {
            Project.TargetFramework = "netstandard2.0";

            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "ComponentLibrary.dll");

            Assert.NupkgDoesNotContain(
                result,
                Path.Combine("bin", Configuration, "ComponentLibrary.1.0.0.nupkg"),
                filePaths: new[]
                {
                    Path.Combine("build", "Microsoft.AspNetCore.StaticWebAssets.props"),
                    Path.Combine("build", "ComponentLibrary.props"),
                    Path.Combine("buildMultiTargeting", "ComponentLibrary.props"),
                    Path.Combine("buildTransitive", "ComponentLibrary.props")
                });
        }

        [Fact]
        [InitializeTestProject("PackageLibraryTransitiveDependency")]
        public async Task Pack_Incremental_DoesNotRegenerateCacheAndPropsFiles()
        {
            Project.TargetFramework = "netstandard2.0";
            var result = await DotnetMSBuild("Pack");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "PackageLibraryTransitiveDependency.dll");

            Assert.FileExists(result, IntermediateOutputPath, "staticwebassets", "msbuild.PackageLibraryTransitiveDependency.Microsoft.AspNetCore.StaticWebAssets.props");
            Assert.FileExists(result, IntermediateOutputPath, "staticwebassets", "msbuild.build.PackageLibraryTransitiveDependency.props");
            Assert.FileExists(result, IntermediateOutputPath, "staticwebassets", "msbuild.buildMultiTargeting.PackageLibraryTransitiveDependency.props");
            Assert.FileExists(result, IntermediateOutputPath, "staticwebassets", "msbuild.buildTransitive.PackageLibraryTransitiveDependency.props");
            Assert.FileExists(result, IntermediateOutputPath, "staticwebassets", "PackageLibraryTransitiveDependency.StaticWebAssets.Pack.cache");

            var directoryPath = Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath, "staticwebassets");
            var thumbPrints = new Dictionary<string, FileThumbPrint>();
            var thumbPrintFiles = new[]
            {
                Path.Combine(directoryPath, "msbuild.PackageLibraryTransitiveDependency.Microsoft.AspNetCore.StaticWebAssets.props"),
                Path.Combine(directoryPath, "msbuild.build.PackageLibraryTransitiveDependency.props"),
                Path.Combine(directoryPath, "msbuild.buildMultiTargeting.PackageLibraryTransitiveDependency.props"),
                Path.Combine(directoryPath, "msbuild.buildTransitive.PackageLibraryTransitiveDependency.props"),
                Path.Combine(directoryPath, "PackageLibraryTransitiveDependency.StaticWebAssets.Pack.cache"),
            };

            foreach (var file in thumbPrintFiles)
            {
                var thumbprint = GetThumbPrint(file);
                thumbPrints[file] = thumbprint;
            }

            // Act
            var incremental = await DotnetMSBuild("Pack");

            // Assert
            Assert.BuildPassed(incremental, allowWarnings: true);

            foreach (var file in thumbPrintFiles)
            {
                var thumbprint = GetThumbPrint(file);
                Assert.Equal(thumbPrints[file], thumbprint);
            }
        }
    }
}
