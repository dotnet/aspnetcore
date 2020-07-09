// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildWithComponents31IntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildWithComponents31IntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("blazor31")]
        public async Task Build_Components_WithDotNetCoreMSBuild_Works()
        {
            Project.TargetFramework = "netcoreapp3.1";
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "blazor31.dll");
            Assert.FileExists(result, OutputPath, "blazor31.pdb");
            Assert.FileExists(result, OutputPath, "blazor31.Views.dll");
            Assert.FileExists(result, OutputPath, "blazor31.Views.pdb");

            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "blazor31.dll"), "blazor31.Pages.Index");
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "blazor31.dll"), "blazor31.Shared.NavMenu");

            // Verify a regular View appears in the views dll, but not in the main assembly.
            Assert.AssemblyDoesNotContainType(result, Path.Combine(OutputPath, "blazor31.dll"), "blazor31.Pages.Pages__Host");
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "blazor31.Views.dll"), "blazor31.Pages.Pages__Host");
        }
    }
}
