// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class ScopedCssIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public ScopedCssIntegrationTest(
            BuildServerTestFixture buildServer,
            ITestOutputHelper output)
            : base(buildServer)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; private set; }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_NoOps_WhenScopedCssIsDisabled()
        {
            var result = await DotnetMSBuild("Build", "/p:ScopedCssEnabled=false");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "ComponentApp.styles.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "FetchData.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task CanDisableDefaultDiscoveryConvention()
        {
            var result = await DotnetMSBuild("Build", "/p:EnableDefaultScopedCssItems=false");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "ComponentApp.styles.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "FetchData.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task CanOverrideScopeIdentifiers()
        {
            var stylesFolder = Path.Combine(Project.DirectoryPath, "Styles", "Pages");
            Directory.CreateDirectory(stylesFolder);
            var styles = Path.Combine(stylesFolder, "Counter.css");
            File.Move(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Counter.razor.css"), styles);
            Project.AddProjectFileContent(@"
<ItemGroup>
    <ScopedCssInput Include=""Styles\Pages\Counter.css"">
        <RazorComponent>Components\Pages\Counter.razor</RazorComponent>
        <CssScope>b-overriden</CssScope>
    </ScopedCssInput>
</ItemGroup>
");
            var result = await DotnetMSBuild("Build", "/p:EnableDefaultScopedCssItems=false /p:_RazorSourceGeneratorWriteGeneratedOutput=true");
            Assert.BuildPassed(result);

            var scoped = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Styles", "Pages", "Counter.rz.scp.css");
            Assert.FileContains(result, scoped, "b-overriden");
            var generated = Assert.FileExists(result, IntermediateOutputPath, "Razor", "Components", "Pages", "Counter.razor.g.cs");
            Assert.FileContains(result, generated, "b-overriden");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_GeneratesTransformedFilesAndBundle_ForComponentsWithScopedCss()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "bundle", "ComponentApp.styles.css");
            Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "projectbundle", "ComponentApp.bundle.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "FetchData.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_ScopedCssFiles_ContainsUniqueScopesPerFile()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            var generatedCounter = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            var generatedIndex = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
            var counterContent = File.ReadAllText(generatedCounter);
            var indexContent = File.ReadAllText(generatedIndex);

            var counterScopeMatch = Regex.Match(counterContent, ".*button\\[(.*)\\].*", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Assert.True(counterScopeMatch.Success, "Couldn't find a scope id in the generated Counter scoped css file.");
            var counterScopeId = counterScopeMatch.Groups[1].Captures[0].Value;

            var indexScopeMatch = Regex.Match(indexContent, ".*h1\\[(.*)\\].*", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Assert.True(indexScopeMatch.Success, "Couldn't find a scope id in the generated Index scoped css file.");
            var indexScopeId = indexScopeMatch.Groups[1].Captures[0].Value;

            Assert.NotEqual(counterScopeId, indexScopeId);
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Publish_PublishesBundleToTheRightLocation()
        {
            var result = await DotnetMSBuild("Publish");
            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "wwwroot", "ComponentApp.styles.css");
            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Counter.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Publish_NoBuild_PublishesBundleToTheRightLocation()
        {
            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            result = await DotnetMSBuild("Publish", "/p:NoBuild=true");
            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "wwwroot", "ComponentApp.styles.css");
            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Counter.razor.rz.scp.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Publish_DoesNotPublishAnyFile_WhenThereAreNoScopedCssFiles()
        {
            File.Delete(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Counter.razor.css"));
            File.Delete(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Index.razor.css"));

            var result = await DotnetMSBuild("Publish");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "_framework", "scoped.styles.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Publish_Publishes_IndividualScopedCssFiles_WhenNoBundlingIsEnabled()
        {
            var result = await DotnetMSBuild("Publish", args: "/p:DisableScopedCssBundling=true");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "ComponentApp.styles.css");

            Assert.FileExists(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileExists(result, PublishOutputPath, "wwwroot", "_content", "ComponentApp", "Components", "Pages", "Counter.razor.rz.scp.css");
        }


        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_GeneratedComponentContainsScope()
        {
            var result = await DotnetMSBuild("Build", "/p:_RazorSourceGeneratorWriteGeneratedOutput=true");
            Assert.BuildPassed(result);

            var generatedCounter = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            Assert.FileExists(result, IntermediateOutputPath, "Razor", "Components", "Pages", "Counter.razor.g.cs");

            var counterContent = File.ReadAllText(generatedCounter);

            var counterScopeMatch = Regex.Match(counterContent, ".*button\\[(.*)\\].*", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Assert.True(counterScopeMatch.Success, "Couldn't find a scope id in the generated Counter scoped css file.");
            var counterScopeId = counterScopeMatch.Groups[1].Captures[0].Value;

            Assert.FileContains(result, Path.Combine(IntermediateOutputPath, "Razor", "Components", "Pages", "Counter.razor.g.cs"), counterScopeId);
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_RemovingScopedCssAndBuilding_UpdatesGeneratedCodeAndBundle()
        {
            var result = await DotnetMSBuild("Build", "/p:_RazorSourceGeneratorWriteGeneratedOutput=true");
            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            var generatedBundle = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "bundle", "ComponentApp.styles.css");
            var generatedProjectBundle = Assert.FileExists(result, IntermediateOutputPath, "scopedcss", "projectbundle", "ComponentApp.bundle.scp.css");
            var generatedCounter = Assert.FileExists(result, IntermediateOutputPath, "Razor", "Components", "Pages", "Counter.razor.g.cs");

            var componentThumbprint = GetThumbPrint(generatedCounter);
            var bundleThumbprint = GetThumbPrint(generatedBundle);

            File.Delete(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Counter.razor.css"));

            result = await DotnetMSBuild("Build", "/p:_RazorSourceGeneratorWriteGeneratedOutput=true");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            generatedCounter = Assert.FileExists(result, IntermediateOutputPath, "Razor", "Components", "Pages", "Counter.razor.g.cs");

            var newComponentThumbprint = GetThumbPrint(generatedCounter);
            var newBundleThumbprint = GetThumbPrint(generatedBundle);

            Assert.NotEqual(componentThumbprint, newComponentThumbprint);
            Assert.NotEqual(bundleThumbprint, newBundleThumbprint);
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Does_Nothing_WhenThereAreNoScopedCssFiles()
        {
            File.Delete(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Counter.razor.css"));
            File.Delete(Path.Combine(Project.DirectoryPath, "Components", "Pages", "Index.razor.css"));

            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Counter.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "Components", "Pages", "Index.razor.rz.scp.css");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "scopedcss", "_framework", "scoped.styles.css");
        }

        [Fact]
        [InitializeTestProject("ComponentApp", language: "C#")]
        public async Task Build_ScopedCssTransformation_AndBundling_IsIncremental()
        {
            // Arrange
            var thumbprintLookup = new Dictionary<string, FileThumbPrint>();

            // Act 1
            var result = await DotnetMSBuild("Build", "/p:_RazorSourceGeneratorWriteGeneratedOutput=true");

            var directoryPath = Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath, "scopedcss");

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var thumbprint = GetThumbPrint(file);
                thumbprintLookup[file] = thumbprint;
            }

            // Assert 1
            Assert.BuildPassed(result);

            // Act & Assert 2
            for (var i = 0; i < 2; i++)
            {
                // We want to make sure nothing changed between multiple incremental builds.
                using (var razorGenDirectoryLock = LockDirectory(RazorIntermediateOutputPath))
                {
                    result = await DotnetMSBuild("Build");
                }

                Assert.BuildPassed(result);
                foreach (var file in files)
                {
                    var thumbprint = GetThumbPrint(file);
                    Assert.Equal(thumbprintLookup[file], thumbprint);
                }
            }
        }
    }
}
