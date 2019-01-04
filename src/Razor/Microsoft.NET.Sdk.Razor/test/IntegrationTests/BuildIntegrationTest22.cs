// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest22 : MSBuildIntegrationTestBase, IClassFixture<LegacyBuildServerTestFixture>
    {
        public BuildIntegrationTest22(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc22")]
        public async Task Building_NETCoreApp22TargetingProject()
        {
            TargetFramework = "netcoreapp2.2";

            // Build
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc22.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc22.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc22.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc22.Views.pdb");
        }
    }
}
