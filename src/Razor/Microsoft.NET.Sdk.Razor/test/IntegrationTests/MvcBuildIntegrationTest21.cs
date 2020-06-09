// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class MvcBuildIntegrationTest21 : MvcBuildIntegrationTestLegacy
    {
        public MvcBuildIntegrationTest21(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        public override string TestProjectName => "SimpleMvc21";
        public override string TargetFramework => "netcoreapp2.1";

        [Fact]
        public async Task Building_WorksWhenMultipleRazorConfigurationsArePresent()
        {
            using (var project = CreateTestProject())
            {
                AddProjectFileContent(@"
<ItemGroup>
    <RazorConfiguration Include=""MVC-2.1"">
      <Extensions>MVC-2.1;$(CustomRazorExtension)</Extensions>
    </RazorConfiguration>
</ItemGroup>");

                // Build
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                Assert.FileExists(result, OutputPath, "SimpleMvc21.dll");
                Assert.FileExists(result, OutputPath, "SimpleMvc21.pdb");
                Assert.FileExists(result, OutputPath, "SimpleMvc21.Views.dll");
                Assert.FileExists(result, OutputPath, "SimpleMvc21.Views.pdb");
            }
        }

        [Fact]
        public virtual async Task Build_DoesNotAddRelatedAssemblyPart_IfToolSetIsNotRazorSdk()
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
        public virtual async Task Publish_NoopsWithMvcRazorCompileOnPublish_False()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Publish", "/p:MvcRazorCompileOnPublish=false");

                Assert.BuildPassed(result);

                // Everything we do should noop - including building the app.
                Assert.FileExists(result, PublishOutputPath, OutputFileName);
                Assert.FileExists(result, PublishOutputPath, $"{TestProjectName}.pdb");
                Assert.FileDoesNotExist(result, PublishOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileDoesNotExist(result, PublishOutputPath, $"{TestProjectName}.Views.pdb");
            }
        }

        [Fact] // This will use the old precompilation tool, RazorSDK shouldn't get involved.
        public virtual async Task Build_WithMvcRazorCompileOnPublish_Noops()
        {
            using (CreateTestProject())
            {
                var result = await DotnetMSBuild("Build", "/p:MvcRazorCompileOnPublish=true");

                Assert.BuildPassed(result);

                Assert.FileExists(result, IntermediateOutputPath, OutputFileName);
                Assert.FileExists(result, IntermediateOutputPath, OutputFileName);
                Assert.FileDoesNotExist(result, IntermediateOutputPath, $"{TestProjectName}.Views.dll");
                Assert.FileDoesNotExist(result, IntermediateOutputPath, $"{TestProjectName}.Views.pdb");
            }
        }
    }
}
