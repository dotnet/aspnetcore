// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest21 : BuildIntegrationTestLegacy
    {
        public BuildIntegrationTest21(LegacyBuildServerTestFixture buildServer)
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
    }
}
