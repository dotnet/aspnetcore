// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIncrementalismTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIncrementalismTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildIncremental_SimpleMvc_PersistsTargetInputFile()
        {
            // Arrange
            var thumbprintLookup = new Dictionary<string, FileThumbPrint>();

            // Act 1
            var result = await DotnetMSBuild("Build");

            var directoryPath = Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath);
            var filesToIgnore = new List<string>()
            {
                // These files are generated on every build.
                Path.Combine(directoryPath, "SimpleMvc.csproj.CopyComplete"),
                Path.Combine(directoryPath, "SimpleMvc.csproj.FileListAbsolute.txt"),
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // There is some quirkiness with MsBuild in unix where it regenerates this file
                // even though it shouldn't. This is tracked here https://github.com/aspnet/Razor/issues/2219.
                filesToIgnore.Add(Path.Combine(directoryPath, "SimpleMvc.TagHelpers.input.cache"));
            }

            var files = Directory.GetFiles(directoryPath).Where(p => !filesToIgnore.Contains(p));
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

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_RegeneratesTagHelperInputs_IfFileChanges()
        {
            // Act - 1
            var expectedTagHelperCacheContent = @"""Name"":""SimpleMvc.SimpleTagHelper""";
            var result = await DotnetMSBuild("Build");
            var file = Path.Combine(Project.DirectoryPath, "SimpleTagHelper.cs");
            var tagHelperOutputCache = Path.Combine(IntermediateOutputPath, "SimpleMvc.TagHelpers.output.cache");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.g.cshtml.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileContains(result, tagHelperOutputCache, expectedTagHelperCacheContent);
            var fileThumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            // Update the source content and build. We should expect the outputs to be regenerated.
            ReplaceContent(string.Empty, file);
            result = await DotnetMSBuild("Build");

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileContentEquals(result, tagHelperOutputCache, "[]");
            var newThumbPrint = GetThumbPrint(generatedFile);
            Assert.NotEqual(fileThumbPrint, newThumbPrint);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ErrorInGeneratedCode_ReportsMSBuildError_OnIncrementalBuild()
        {
            // Introducing a Razor semantic error
            ReplaceContent("@{ // Unterminated code block", "Views", "Home", "Index.cshtml");

            // Regular build
            await VerifyError();

            // Incremental build
            await VerifyError();

            async Task VerifyError()
            {
                var result = await DotnetMSBuild("Build");

                Assert.BuildFailed(result);

                // This needs to be relative path. Tracked by https://github.com/aspnet/Razor/issues/2187.
                var filePath = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
                var location = filePath + "(1,2)";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Absolute paths on OSX don't work well.
                    location = null;
                }

                Assert.BuildError(result, "RZ1006", location: location);

                // Compilation failed without creating the views assembly
                Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
                Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

                // File with error does not get written to disk.
                Assert.FileDoesNotExist(result, IntermediateOutputPath, "Razor", "Views", "Home", "Index.cshtml.g.cs");
            }
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task IncrementalBuild_WithP2P_WorksWhenBuildProjectReferencesIsDisabled()
        {
            // Simulates building the same way VS does by setting BuildProjectReferences=false.
            // With this flag, the only target called is GetCopyToOutputDirectoryItems on the referenced project.
            // We need to ensure that we continue providing Razor binaries and symbols as files to be copied over.
            var result = await DotnetMSBuild(target: default);

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.pdb");

            result = await DotnetMSBuild(target: "Clean", "/p:BuildProjectReferences=false", suppressRestore: true);
            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, OutputPath, "AppWithP2PReference.dll");
            Assert.FileDoesNotExist(result, OutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.Views.pdb");

            // dotnet msbuild /p:BuildProjectReferences=false
            result = await DotnetMSBuild(target: default, "/p:BuildProjectReferences=false", suppressRestore: true);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.pdb");
        }
    }
}
