// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorGenerateIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorGenerate_Success_GeneratesFilesOnDisk()
        {
            var result = await DotnetMSBuild("RazorGenerate");

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
    }
}
