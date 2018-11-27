// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
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
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cshtml.g.cs");
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

        [Fact(Skip = "https://github.com/aspnet/Razor/issues/2723")]
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

        // Skipping on MacOS because of https://github.com/dotnet/corefx/issues/33141.
        // Skipping on Linux because of https://github.com/aspnet/Razor/issues/2525.
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject("SimpleMvc")]
        public async Task ManualServerShutdown_NoPipeName_ShutsDownServer()
        {
            // We are trying to test whether the correct pipe name is generated (from the location of rzc tool)
            // when we don't explicitly specify a pipe name.

            // Publish rzc tool to a temporary path. This is the location based on which the pipe name is generated.
            var solutionRoot = TestPathUtilities.GetSolutionRootDirectory("Razor");
            var toolAssemblyDirectory = Path.Combine(solutionRoot, "src", "Microsoft.AspNetCore.Razor.Tools");
            var toolAssemblyPath = Path.Combine(toolAssemblyDirectory, "Microsoft.AspNetCore.Razor.Tools.csproj");
            var projectDirectory = new TestProjectDirectory(solutionRoot, toolAssemblyDirectory, toolAssemblyPath);
            var publishDir = Path.Combine(Path.GetTempPath(), "Razor", Path.GetRandomFileName(), "RzcPublish");
            var publishResult = await MSBuildProcessManager.RunProcessAsync(projectDirectory, $"/t:Publish /p:PublishDir=\"{publishDir}\"");

            try
            {
                // Make sure publish succeeded.
                Assert.BuildPassed(publishResult);

                // Run the build using the published tool
                var toolAssembly = Path.Combine(publishDir, "rzc.dll");
                var result = await DotnetMSBuild(
                    "Build",
                    $"/p:_RazorForceBuildServer=true /p:_RazorToolAssembly={toolAssembly}",
                    suppressBuildServer: true); // We don't want to specify a pipe name

                Assert.BuildPassed(result);

                // Manually shutdown the server
                var processStartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = publishDir,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = "dotnet",
                    Arguments = $"{toolAssembly} shutdown -w"
                };

                var logFilePath = Path.Combine(publishDir, "out.log");
                processStartInfo.Environment.Add("RAZORBUILDSERVER_LOG", logFilePath);
                var shutdownResult = await MSBuildProcessManager.RunProcessCoreAsync(processStartInfo);

                Assert.Equal(0, shutdownResult.ExitCode);
                var output = await File.ReadAllTextAsync(logFilePath);
                Assert.Contains("shut down completed", output);
            }
            finally
            {
                // Finally delete the temporary publish directory
                ProjectDirectory.CleanupDirectory(publishDir);
            }
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

        [Fact]
        [InitializeTestProject("MvcWithComponents")]
        public async Task Build_MvcWithComponents()
        {
            var tagHelperOutputCacheFile = Path.Combine(IntermediateOutputPath, "MvcWithComponents.TagHelpers.output.cache");

            var result = await DotnetMSBuild(
                "Build",
                "/p:_RazorForceBuildServer=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "MvcWithComponents.dll");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.pdb");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.Views.dll");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.Views.pdb");

            // Verify tag helper discovery from components work
            Assert.FileExists(result, tagHelperOutputCacheFile);
            Assert.FileContains(
                result,
                tagHelperOutputCacheFile,
                @"""Name"":""MvcWithComponents.TestComponent""");

            Assert.FileContains(
                result,
                tagHelperOutputCacheFile,
                @"""Name"":""MvcWithComponents.Views.Shared.NavMenu""");
        }

        private class TestProjectDirectory : ProjectDirectory
        {
            public TestProjectDirectory(string solutionPath, string directoryPath, string projectFilePath)
                : base(solutionPath, directoryPath, projectFilePath)
            {
            }
        }
    }
}
