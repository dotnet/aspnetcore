// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntrospectionTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIntrospectionTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc11")]
        public async Task RazorSdk_DoesNotAddCoreRazorConfigurationTo11Projects()
        {
            var result = await DotnetMSBuild("_IntrospectProjectCapabilityItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazor");
            Assert.BuildOutputDoesNotContainLine(result, "ProjectCapability: DotNetCoreRazorConfiguration");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsProjectCapabilities()
        {
            var result = await DotnetMSBuild("_IntrospectProjectCapabilityItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazor");
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazorConfiguration");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsCshtmlFilesToUpToDateCheckInput()
        {
            var result = await DotnetMSBuild("_IntrospectUpToDateCheck");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckInput: {Path.Combine("Views", "Home", "Index.cshtml")}");
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckInput: {Path.Combine("Views", "_ViewStart.cshtml")}");
            Assert.BuildOutputContainsLine(result, $"UpToDateCheckBuilt: {Path.Combine(IntermediateOutputPath, "SimpleMvc.Views.dll")}");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_AddsGeneratedRazorFilesAndAssemblyInfoToRazorCompile()
        {
            var result = await DotnetMSBuild("Build", "/t:_IntrospectRazorCompileItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, $"RazorCompile: {Path.Combine(IntermediateOutputPath, "Razor", "Views", "Home", "Index.cshtml.g.cs")}");
            Assert.BuildOutputContainsLine(result, $"RazorCompile: {Path.Combine(IntermediateOutputPath, "SimpleMvc.RazorTargetAssemblyInfo.cs")}");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorSdk_UsesUseSharedCompilationToSetDefaultValueOfUseRazorBuildServer()
        {
            var result = await DotnetMSBuild("Build", "/t:_IntrospectUseRazorBuildServer /p:UseSharedCompilation=false");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "UseRazorBuildServer: false");
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task GetCopyToOutputDirectoryItems_WhenNoFileIsPresent_ReturnsEmptySequence()
        {
            var result = await DotnetMSBuild(target: default);

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");

            result = await DotnetMSBuild(target: "GetCopyToOutputDirectoryItems", "/t:_IntrospectGetCopyToOutputDirectoryItems /p:BuildProjectReferences=false", suppressRestore: true);
            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.dll");
            Assert.BuildOutputContainsLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.pdb");

            // Remove all views from the class library
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            // dotnet msbuild /p:BuildProjectReferences=false
            result = await DotnetMSBuild(target: "GetCopyToOutputDirectoryItems", "/t:_IntrospectGetCopyToOutputDirectoryItems /p:BuildProjectReferences=false", suppressRestore: true);

            Assert.BuildOutputDoesNotContainLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.dll");
            Assert.BuildOutputDoesNotContainLine(result, "AllItemsFullPathWithTargetPath: ClassLibrary.Views.pdb");
        }
    }
}
