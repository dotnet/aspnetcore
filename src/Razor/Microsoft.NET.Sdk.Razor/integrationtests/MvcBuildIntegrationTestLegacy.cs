// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class MvcBuildIntegrationTestLegacy :
        MSBuildIntegrationTestBase,
        IClassFixture<LegacyBuildServerTestFixture>
    {
        public abstract string TestProjectName { get; }
        public abstract new string TargetFramework { get; }
        public virtual string OutputFileName => $"{TestProjectName}.dll";

        public MvcBuildIntegrationTestLegacy(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        protected IDisposable CreateTestProject()
        {
            Project = ProjectDirectory.Create(TestProjectName);
            MSBuildIntegrationTestBase.TargetFramework = TargetFramework;

            return new Disposable();
        }

        [Fact]
        [QuarantinedTest]
        public virtual async Task Building_Project()
        {
            using (CreateTestProject())
            {
                // Build
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                Assert.FileExists(result, OutputPath, OutputFileName);
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
        public virtual async Task BuildingProject_CopyToOutputDirectoryFiles()
        {
            using (CreateTestProject())
            {
                // Build
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                // No cshtml files should be in the build output directory
                Assert.FileCountEquals(result, 0, Path.Combine(OutputPath, "Views"), "*.cshtml");

                // For .NET Core projects, no ref assemblies should be present in the output directory.
                Assert.FileCountEquals(result, 0, Path.Combine(OutputPath, "refs"), "*.dll");
            }
        }

        [Fact]
        public virtual async Task Publish_Project()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Publish");

                Assert.BuildPassed(result);

                Assert.FileExists(result, PublishOutputPath, OutputFileName);
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.pdb");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.Views.pdb");

                // By default refs and .cshtml files will not be copied on publish
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
            }
        }

        [Fact]
        public virtual async Task Build_DoesNotPrintsWarnings_IfProjectFileContainsRazorFiles()
        {
            using (CreateTestProject())
            {
                File.WriteAllText(Path.Combine(Project.DirectoryPath, "Index.razor"), "Hello world");
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                Assert.DoesNotContain("RAZORSDK1005", result.Output);
            }
        }

        [Fact]
        public async Task PublishingProject_CopyToPublishDirectoryItems()
        {
            using (CreateTestProject())
            {
                // Build
                var result = await DotnetMSBuild("Publish");

                Assert.BuildPassed(result);

                // refs shouldn't be produced by default
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");

                // Views shouldn't be produced by default
                Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
            }
        }

        [Fact]
        public virtual async Task Publish_IncludesRefAssemblies_WhenCopyRefAssembliesToPublishDirectoryIsSet()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Publish", "/p:CopyRefAssembliesToPublishDirectory=true");

                Assert.BuildPassed(result);
                Assert.FileExists(result, PublishOutputPath, "refs", "System.Threading.Tasks.Extensions.dll");
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
