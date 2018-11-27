// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIncrementalismTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIncrementalismTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "See https://github.com/aspnet/Razor/issues/2219")]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildIncremental_SimpleMvc_PersistsTargetInputFile()
        {
            // Arrange
            var thumbprintLookup = new Dictionary<string, FileThumbPrint>();

            // Act 1
            var result = await DotnetMSBuild("Build");

            var directoryPath = Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath);
            var filesToIgnore = new[]
            {
                // These files are generated on every build.
                Path.Combine(directoryPath, "SimpleMvc.csproj.CopyComplete"),
                Path.Combine(directoryPath, "SimpleMvc.csproj.FileListAbsolute.txt"),
            };

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

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Build_TouchesUpToDateMarkerFile()
        {
            var classLibraryDll = Path.Combine(IntermediateOutputPath, "ClassLibrary.dll");
            var classLibraryViewsDll = Path.Combine(IntermediateOutputPath, "ClassLibrary.Views.dll");
            var markerFile = Path.Combine(IntermediateOutputPath, "ClassLibrary.csproj.CopyComplete");

            var result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            Assert.FileExists(result, classLibraryDll);
            Assert.FileExists(result, classLibraryViewsDll);
            Assert.FileExists(result, markerFile);

            // Gather thumbprints before incremental build.
            var classLibraryThumbPrint = GetThumbPrint(classLibraryDll);
            var classLibraryViewsThumbPrint = GetThumbPrint(classLibraryViewsDll);
            var markerFileThumbPrint = GetThumbPrint(markerFile);

            result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            // Verify thumbprint file is unchanged between true incremental builds
            Assert.Equal(classLibraryThumbPrint, GetThumbPrint(classLibraryDll));
            Assert.Equal(classLibraryViewsThumbPrint, GetThumbPrint(classLibraryViewsDll));
            // In practice, this should remain unchanged. However, since our tests reference
            // binaries from other projects, this file gets updated by Microsoft.Common.targets
            Assert.NotEqual(markerFileThumbPrint, GetThumbPrint(markerFile));

            // Change a cshtml file and verify ClassLibrary.Views.dll and marker file are updated
            File.AppendAllText(Path.Combine(Project.DirectoryPath, "Views", "_ViewImports.cshtml"), Environment.NewLine);

            result = await DotnetMSBuild("Build");
            Assert.BuildPassed(result);

            Assert.Equal(classLibraryThumbPrint, GetThumbPrint(classLibraryDll));
            Assert.NotEqual(classLibraryViewsThumbPrint, GetThumbPrint(classLibraryViewsDll));
            Assert.NotEqual(markerFileThumbPrint, GetThumbPrint(markerFile));
        }
    }
}
