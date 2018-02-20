// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildServerIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        private readonly string _pipeName;

        public BuildServerIntegrationTest(BuildServerTestFixture fixture)
        {
            _pipeName = fixture.PipeName;
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public Task Build_SimpleMvc_WithServer_UsingDotnetMSBuild_CanBuildSuccessfully()
            => Build_SimpleMvc_CanBuildSuccessfully(MSBuildProcessKind.Dotnet);

        private async Task Build_SimpleMvc_CanBuildSuccessfully(MSBuildProcessKind msBuildProcessKind)
        {
            var result = await DotnetMSBuild(
                "Build", 
                $"/p:RazorCompileOnBuild=true /p:UseRazorBuildServer=true /p:_RazorBuildServerPipeName={_pipeName} /p:_RazorForceBuildServer=true",
                msBuildProcessKind: msBuildProcessKind);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject(originalProjectName: "SimpleMvc", targetProjectName: "SimpleMvc", baseDirectory: "Whitespace in path")]
        public async Task Build_AppWithWhitespaceInPath_CanBuildSuccessfully()
        {
            var result = await DotnetMSBuild(
                "Build",
                $"/p:RazorCompileOnBuild=true /p:UseRazorBuildServer=true /p:_RazorBuildServerPipeName={_pipeName} /p:_RazorForceBuildServer=true");

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
                $"/p:RazorCompileOnBuild=true /p:UseRazorBuildServer=true /p:_RazorBuildServerPipeName={_pipeName} /p:_RazorForceBuildServer=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "Whitespace in name.dll");
            Assert.FileExists(result, OutputPath, "Whitespace in name.pdb");
            Assert.FileExists(result, OutputPath, "Whitespace in name.Views.dll");
            Assert.FileExists(result, OutputPath, "Whitespace in name.Views.pdb");

            Assert.FileExists(result, IntermediateOutputPath, "Whitespace in name.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "Whitespace in name.RazorCoreGenerate.cache");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
        }
    }
}
