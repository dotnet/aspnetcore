// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
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
    }
}
