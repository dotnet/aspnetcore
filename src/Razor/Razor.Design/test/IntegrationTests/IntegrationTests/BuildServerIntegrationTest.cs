// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Tools;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.CodeAnalysis;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildServerIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        private BuildServerTestFixture _buildServer;

        public BuildServerIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
            _buildServer = buildServer;
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

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ServerConnectionMutexCreationFails_FallsBackToInProcessRzc()
        {
            // Use a pipe name longer that 260 characters to make the Mutex constructor throw.
            var pipeName = new string('P', 261);
            var result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=true",
                buildServerPipeName: pipeName);

            // We expect this to fail because we don't allow it to fallback to in process execution.
            Assert.BuildFailed(result);
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            // Try to build again without forcing it to run on build server.
            result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=false",
                buildServerPipeName: pipeName);

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            // Note: We don't need to handle server clean up here because it will fail before
            // it reaches server creation part.
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task ManualServerShutdown_NoPipeName_ShutsDownServer()
        {
            var toolAssembly = typeof(Application).Assembly.Location;
            var result = await DotnetMSBuild(
                "Build",
                $"/p:_RazorForceBuildServer=true /p:_RazorToolAssembly={toolAssembly}",
                suppressBuildServer: true); // We don't want to specify a pipe name

            Assert.BuildPassed(result);

            // Shutdown the server
            var output = new StringWriter();
            var error = new StringWriter();
            var application = new Application(CancellationToken.None, Mock.Of<ExtensionAssemblyLoader>(), Mock.Of<ExtensionDependencyChecker>(), (path, properties) => Mock.Of<PortableExecutableReference>(), output, error);
            var exitCode = application.Execute("shutdown", "-w");

            Assert.Equal(0, exitCode);
            Assert.Contains("shut down completed", output.ToString());
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_WithWhiteSpaceInPipeName_BuildsSuccessfully()
        {
            // Start the server
            var pipeName = "pipe with whitespace";
            var fixture = new BuildServerTestFixture(pipeName);

            try
            {
                // Run a build
                var result = await DotnetMSBuild(
                    "Build",
                    "/p:_RazorForceBuildServer=true",
                    buildServerPipeName: pipeName);

                Assert.BuildPassed(result);
                Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
                Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
                Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
                Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");
            }
            finally
            {
                // Shutdown the server
                fixture.Dispose();
            }
        }
    }
}
