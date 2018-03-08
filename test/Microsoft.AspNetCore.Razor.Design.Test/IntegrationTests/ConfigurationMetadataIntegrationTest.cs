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
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs");
            Assert.FileContainsLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorLanguageVersionAttribute(\"2.1\")]");
            Assert.FileContainsLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorConfigurationNameAttribute(\"MVC-2.1\")]");
            Assert.FileContainsLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorExtensionAssemblyNameAttribute(\"MVC-2.1\", \"Microsoft.AspNetCore.Mvc.Razor.Extensions\")]");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_WithGenerateRazorAssemblyInfo_False_SuppressesConfigurationMetadata()
        {
            var result = await DotnetMSBuild("Build", "/p:GenerateRazorAssemblyInfo=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorLanguageVersionAttribute(\"2.1\")]");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorConfigurationNameAttribute(\"MVC-2.1\")]");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "SimpleMvc.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorExtensionAssemblyNameAttribute(\"MVC-2.1\", \"Microsoft.AspNetCore.Razor.Extensions\")]");
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Build_ForClassLibrary_SuppressesConfigurationMetadata()
        {
            TargetFramework = "netstandard2.0";

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "ClassLibrary.Views.pdb");

            Assert.FileExists(result, IntermediateOutputPath, "ClassLibrary.AssemblyInfo.cs");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "ClassLibrary.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorLanguageVersionAttribute(\"2.1\")]");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "ClassLibrary.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorConfigurationNameAttribute(\"MVC-2.1\")]");
            Assert.FileDoesNotContainLine(
                result,
                Path.Combine(IntermediateOutputPath, "ClassLibrary.AssemblyInfo.cs"),
                "[assembly: Microsoft.AspNetCore.Razor.Hosting.RazorExtensionAssemblyNameAttribute(\"MVC-2.1\", \"Microsoft.AspNetCore.Razor.Extensions\")]");
        }
    }
}
