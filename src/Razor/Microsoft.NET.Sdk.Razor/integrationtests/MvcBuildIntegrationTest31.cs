// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class MvcBuildIntegrationTest31 : MvcBuildIntegrationTestLegacy
    {
        public MvcBuildIntegrationTest31(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        public override string TestProjectName => "SimpleMvc31";
        public override string TargetFramework => "netcoreapp3.1";

        [Fact]
        public async Task Build_WithGenerateRazorHostingAssemblyInfo_AddsConfigurationMetadata()
        {
            using var project = CreateTestProject();

            var razorAssemblyInfo = Path.Combine(IntermediateOutputPath, "SimpleMvc31.RazorAssemblyInfo.cs");
            var result = await DotnetMSBuild("Build", "/p:GenerateRazorHostingAssemblyInfo=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc31.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc31.Views.pdb");

            Assert.FileExists(result, razorAssemblyInfo);
            Assert.FileContainsLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorLanguageVersionAttribute(\"3.0\")]");
            Assert.FileContainsLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorConfigurationNameAttribute(\"MVC-3.0\")]");
            Assert.FileContainsLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorExtensionAssemblyNameAttribute(\"MVC-3.0\", \"Microsoft.AspNetCore.Mvc.Razor.Extensions\")]");
        }
    }
}
