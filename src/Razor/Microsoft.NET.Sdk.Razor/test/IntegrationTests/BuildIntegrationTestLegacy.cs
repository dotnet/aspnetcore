// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class BuildIntegrationTestLegacy :
        MSBuildIntegrationTestBase,
        IClassFixture<LegacyBuildServerTestFixture>
    {
        public abstract string TestProjectName { get; }
        public abstract new string TargetFramework { get; }

        public BuildIntegrationTestLegacy(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        protected IDisposable CreateTestProject()
        {
            Project = ProjectDirectory.Create(TestProjectName, TestProjectName, string.Empty, Array.Empty<string>(), "C#");
            MSBuildIntegrationTestBase.TargetFramework = TargetFramework;

            return new Disposable();
        }

        [Fact]
        public async Task Building_Project()
        {
            using (CreateTestProject())
            {
                // Build
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                Assert.FileExists(result, OutputPath, $"{TestProjectName}.dll");
                Assert.FileExists(result, OutputPath, $"{TestProjectName}.pdb");
                Assert.FileExists(result, OutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileExists(result, OutputPath, $"{TestProjectName}.Views.pdb");

                // Verify RazorTagHelper works
                Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.TagHelpers.input.cache");
                Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.TagHelpers.output.cache");
                Assert.FileContains(
                    result,
                    Path.Combine(IntermediateOutputPath, $"{TestProjectName}.TagHelpers.output.cache"),
                    @"""Name"":""SimpleMvc.SimpleTagHelper""");
            }
        }

        [Fact]
        public async Task Publish_Project()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Publish");

                Assert.BuildPassed(result);

                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.dll");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.pdb");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.Views.pdb");

                // By default refs and .cshtml files will not be copied on publish
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
            }
        }

        [Fact]
        public async Task Publish_NoopsWithMvcRazorCompileOnPublish_False()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Publish", "/p:MvcRazorCompileOnPublish=false");

                Assert.BuildPassed(result);

                // Everything we do should noop - including building the app.
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.dll");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.pdb");
                Assert.FileDoesNotExist(result, PublishOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileDoesNotExist(result, PublishOutputPath, $"{TestProjectName}.Views.pdb");
            }
        }

        [Fact] // This will use the old precompilation tool, RazorSDK shouldn't get involved.
        public async Task Build_WithMvcRazorCompileOnPublish_Noops()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Build", "/p:MvcRazorCompileOnPublish=true");

                Assert.BuildPassed(result);

                Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.dll");
                Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.dll");
                Assert.FileDoesNotExist(result, IntermediateOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileDoesNotExist(result, IntermediateOutputPath, $"{TestProjectName}.Views.pdb");
            }
        }

        [Fact]
        public async Task Build_DoesNotAddRelatedAssemblyPart_IfToolSetIsNotRazorSdk()
        {
            using (CreateTestProject())
            {
                var razorAssemblyInfo = Path.Combine(IntermediateOutputPath, $"{TestProjectName}.RazorAssemblyInfo.cs");
                var result = await DotnetMSBuild("Build", "/p:RazorCompileToolSet=MvcPrecompilation");

                Assert.BuildPassed(result);

                Assert.FileDoesNotExist(result, razorAssemblyInfo);
                Assert.FileDoesNotExist(result, IntermediateOutputPath, $"{TestProjectName}.RazorTargetAssemblyInfo.cs");
            }
        }

        [Fact]
        public async Task Build_DoesNotPrintsWarnings_IfProjectFileContainsRazorFiles()
        {
            using (CreateTestProject())
            {
                File.WriteAllText(Path.Combine(Project.DirectoryPath, "Index.razor"), "Hello world");
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                Assert.DoesNotContain("RAZORSDK1005", result.Output);
            }
        }

        private class Disposable : IDisposable
        {
            public void Dispose()
            {
                Project.Dispose();
                Project = null;
            }
        }
    }
}
