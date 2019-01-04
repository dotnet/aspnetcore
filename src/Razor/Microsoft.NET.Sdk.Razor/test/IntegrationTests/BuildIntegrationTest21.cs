// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest21 : MSBuildIntegrationTestBase, IClassFixture<LegacyBuildServerTestFixture>
    {
        public BuildIntegrationTest21(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc21")]
        public async Task Building_NETCoreApp21TargetingProject()
        {
            TargetFramework = "netcoreapp2.1";

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
