// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_SimpleMvc_CanBuildSuccessfully()
        {
            var result = await DotnetMSBuild("Build", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.PrecompiledViews.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ErrorInGeneratedCode_ReportsMSBuildError()
        {
            // Introducing a C# semantic error
            ReplaceContent("@{ var foo = \"\".Substring(\"bleh\"); }", "Views", "Home", "Index.cshtml");

            var result = await DotnetMSBuild("Build", "/p:RazorCompileOnBuild=true");

            Assert.BuildFailed(result);

            // Verifying that the error correctly gets mapped to the original source
            Assert.BuildError(result, "CS1503", location: Path.Combine("Views", "Home", "Index.cshtml") + "(1,27)");

            // Compilation failed without creating the views assembly
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");
        }
    }
}
