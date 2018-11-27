// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class ConfigurationMetadataIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public ConfigurationMetadataIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_WithMvc_AddsConfigurationMetadata()
        {
            var razorAssemblyInfo = Path.Combine(IntermediateOutputPath, "SimpleMvc.RazorAssemblyInfo.cs");
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

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

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Build_ForClassLibrary_SuppressesConfigurationMetadata()
        {
            var razorAssemblyInfo = Path.Combine(IntermediateOutputPath, "ClassLibrary.RazorAssemblyInfo.cs");
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "ClassLibrary.Views.pdb");

            Assert.FileExists(result, razorAssemblyInfo);
            Assert.FileDoesNotContainLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorLanguageVersionAttribute(\"3.0\")]");
            Assert.FileDoesNotContainLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorConfigurationNameAttribute(\"MVC-3.0\")]");
            Assert.FileDoesNotContainLine(
                result,
                razorAssemblyInfo,
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorExtensionAssemblyNameAttribute(\"MVC-3.0\", \"Microsoft.AspNetCore.Razor.Extensions\")]");
        }
    }
}
