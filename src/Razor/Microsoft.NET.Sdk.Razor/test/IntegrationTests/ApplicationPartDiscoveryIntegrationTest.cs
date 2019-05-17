// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class ApplicationPartDiscoveryIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public ApplicationPartDiscoveryIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public Task Build_ProjectWithDependencyThatReferencesMvc_AddsAttribute_WhenBuildingUsingDotnetMsbuild()
            => Build_ProjectWithDependencyThatReferencesMvc_AddsAttribute(MSBuildProcessKind.Dotnet);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public Task Build_ProjectWithDependencyThatReferencesMvc_AddsAttribute_WhenBuildingUsingDesktopMsbuild()
            => Build_ProjectWithDependencyThatReferencesMvc_AddsAttribute(MSBuildProcessKind.Desktop);

        private async Task Build_ProjectWithDependencyThatReferencesMvc_AddsAttribute(MSBuildProcessKind msBuildProcessKind)
        {
            var result = await DotnetMSBuild("Build", msBuildProcessKind: msBuildProcessKind);

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs");
            Assert.FileContains(result, Path.Combine(IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs"), "[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute(\"ClassLibrary\")]");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ProjectWithoutMvcReferencingDependencies_DoesNotGenerateAttribute()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cs");
            // We should produced an empty file for build incrementalism
            Assert.Empty(File.ReadAllText(Path.Combine(result.Project.DirectoryPath, IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cs")));
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task BuildIncrementalism_WhenApplicationPartAttributeIsGenerated()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            var generatedAttributeFile = Path.Combine(IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs");
            Assert.FileExists(result, generatedAttributeFile);
            Assert.FileContains(result, generatedAttributeFile, "[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute(\"ClassLibrary\")]");

            var thumbPrint = GetThumbPrint(generatedAttributeFile);

            result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, generatedAttributeFile);
            Assert.Equal(thumbPrint, GetThumbPrint(generatedAttributeFile));
        }

        [Fact]
        [InitializeTestProject("SimpleMvcFSharp", language: "F#", additionalProjects: "ClassLibrary")]
        public async Task Build_ProjectWithDependencyThatReferencesMvc_AddsAttributeToNonCSharpProjects()
        {
            AddProjectFileContent(
@"
    <ItemGroup>
        <ProjectReference Include=""..\ClassLibrary\ClassLibrary.csproj"" />
    </ItemGroup>
");

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvcFSharp.MvcApplicationPartsAssemblyInfo.fs");
            Assert.FileContains(result, Path.Combine(IntermediateOutputPath, "SimpleMvcFSharp.MvcApplicationPartsAssemblyInfo.fs"), "<assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute(\"ClassLibrary\")>");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildIncrementalism_WhenApplicationPartAttributeIsNotGenerated()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            var generatedAttributeFile = Path.Combine(IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cs");
            Assert.FileExists(result, generatedAttributeFile);
            Assert.Empty(File.ReadAllText(Path.Combine(result.Project.DirectoryPath, generatedAttributeFile)));

            var thumbPrint = GetThumbPrint(generatedAttributeFile);

            result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, generatedAttributeFile);
            Assert.Equal(thumbPrint, GetThumbPrint(generatedAttributeFile));
        }
    }
}
