// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildServerIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildServerIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public Task Build_SimpleMvc_WithServer_UsingDotnetMSBuild_CanBuildSuccessfully()
            => Build_SimpleMvc_CanBuildSuccessfully(MSBuildProcessKind.Dotnet);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject("SimpleMvc")]
        public Task Build_SimpleMvc_WithServer_UsingDesktopMSBuild_CanBuildSuccessfully()
            => Build_SimpleMvc_CanBuildSuccessfully(MSBuildProcessKind.Desktop);

        private async Task Build_SimpleMvc_CanBuildSuccessfully(MSBuildProcessKind msBuildProcessKind)
        {
            var result = await DotnetMSBuild(
                "Build", 
                "/p:_RazorForceBuildServer=true",
                msBuildProcessKind: msBuildProcessKind);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");

            // Verify RazorTagHelper works
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.TagHelpers.input.cache");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache");
            Assert.FileContains(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache"),
                @"""Name"":""SimpleMvc.SimpleTagHelper""");

            // Verify RazorGenerate works
            Assert.FileCountEquals(result, 8, RazorIntermediateOutputPath, "*.cs");
        }

        [Fact]
        [InitializeTestProject(originalProjectName: "SimpleMvc", targetProjectName: "SimpleMvc", baseDirectory: "Whitespace in path")]
        public async Task Build_AppWithWhitespaceInPath_CanBuildSuccessfully()
        {
            var result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject(originalProjectName: "SimpleMvc", targetProjectName: "Whitespace in name", baseDirectory: "")]
        public async Task Build_AppWithWhitespaceInName_CanBuildSuccessfully()
        {
            var result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "Whitespace in name.dll");
            Assert.FileExists(result, OutputPath, "Whitespace in name.pdb");
            Assert.FileExists(result, OutputPath, "Whitespace in name.Views.dll");
            Assert.FileExists(result, OutputPath, "Whitespace in name.Views.pdb");

            Assert.FileExists(result, IntermediateOutputPath, "Whitespace in name.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "Whitespace in name.RazorCoreGenerate.cache");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.g.cshtml.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ErrorInServer_DisplaysErrorInMsBuildOutput()
        {
            var result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=true /p:RazorLangVersion=5.0");

            Assert.BuildFailed(result);
            Assert.BuildOutputContainsLine(
                result,
                $"Invalid option 5.0 for Razor language version --version; must be Latest or a valid version in range {RazorLanguageVersion.Version_1_0} to {RazorLanguageVersion.Latest}.");

            // Compilation failed without creating the views assembly
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
        }
    }
}
