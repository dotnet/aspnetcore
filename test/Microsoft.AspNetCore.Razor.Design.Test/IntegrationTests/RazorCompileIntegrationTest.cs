// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorCompileIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorCompile_Success_CompilesAssembly()
        {
            var result = await DotnetMSBuild("RazorCompile");

            Assert.BuildPassed(result);

            // RazorGenerate should compile the assembly and pdb.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorCompile_NoopsWithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("RazorCompile");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }
    }
}
