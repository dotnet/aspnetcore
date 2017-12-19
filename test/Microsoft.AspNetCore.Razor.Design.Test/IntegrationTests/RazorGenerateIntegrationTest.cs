// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorGenerateIntegrationTest : MSBuildIntegrationTestBase
    {
        private const string RazorGenerateTarget = "RazorGenerate";

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Success_GeneratesFilesOnDisk()
        {
            var result = await DotnetMSBuild(RazorGenerateTarget);

            Assert.BuildPassed(result);

            // RazorGenerate should compile the assembly, but not the views.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");

            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewImports.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "_ViewStart.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "About.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Contact.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_Layout.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "_ValidationScriptsPartial.cs");
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Shared", "Error.cs");
            Assert.FileCountEquals(result, 8, RazorIntermediateOutputPath, "*.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_ErrorInRazorFile_ReportsMSBuildError()
        {
            // Introducing a syntax error, an unclosed brace
            ReplaceContent("@{", "Views", "Home", "Index.cshtml");

            var result = await DotnetMSBuild("RazorGenerate");

            Assert.BuildFailed(result);

            // Looks like C:\...\Views\Home\Index.cshtml(1,2): error RZ1006: The code block is missi... [C:\Users\rynowak\AppData\Local\Temp\rwnv03ll.wb0\SimpleMvc.csproj]
            Assert.BuildError(result, "RZ1006");

            // RazorGenerate should compile the assembly, but not the views.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");

            // The file should still be generated even if we had a Razor syntax error.
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "Index.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_BuildsIncrementally()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var generatedFile = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);
            var thumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            using (var razorGenDirectoryLock = LockDirectory(RazorIntermediateOutputPath))
            {
                result = await DotnetMSBuild(RazorGenerateTarget);
            }

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);
            var currentThumbPrint = GetThumbPrint(generatedFile);
            Assert.Equal(thumbPrint, currentThumbPrint);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfSourcesAreUpdated()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Contact.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Contact.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            var fileThumbPrint = GetThumbPrint(generatedFile);

            // Act - 2
            // Update the source content and build. We should expect the outputs to be regenerated.
            // Timestamps on xplat are precise only to a second. Add a delay so we can ensure that MSBuild recognizes the
            // file change. See https://github.com/dotnet/corefx/issues/26024
            await Task.Delay(TimeSpan.FromSeconds(1));
            ReplaceContent("Uodated content", file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            var newThumbPrint = GetThumbPrint(generatedFile);
            Assert.NotEqual(fileThumbPrint, newThumbPrint);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfOutputFilesAreMissing()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, RazorIntermediateOutputPath, "Views", "Home", "About.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreRenamed()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var renamed = Path.Combine(Project.DirectoryPath, "Views", "Home", "NewIndex.cshtml");
            var generated = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, file);
            Assert.FileExists(result, generated);

            // Act - 2
            File.Move(file, renamed);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileExists(result, RazorIntermediateOutputPath, "Views", "Home", "NewIndex.cs");
            Assert.FileDoesNotExist(result, generated);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Rebuilds_IfInputFilesAreDeleted()
        {
            // Act - 1
            var result = await DotnetMSBuild(RazorGenerateTarget);
            var file = Path.Combine(Project.DirectoryPath, "Views", "Home", "Index.cshtml");
            var generatedFile = Path.Combine(RazorIntermediateOutputPath, "Views", "Home", "Index.cs");

            // Assert - 1
            Assert.BuildPassed(result);
            Assert.FileExists(result, generatedFile);

            // Act - 2
            File.Delete(file);
            result = await DotnetMSBuild(RazorGenerateTarget);

            // Assert - 2
            Assert.BuildPassed(result);
            Assert.FileDoesNotExist(result, generatedFile);
        }
    }
}
