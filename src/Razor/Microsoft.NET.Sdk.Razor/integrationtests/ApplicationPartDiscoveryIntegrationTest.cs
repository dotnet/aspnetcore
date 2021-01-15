// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
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

        [ConditionalFact(Skip = "net5.0 TFM is not recognized on Desktop MSBuild. A VS update will be needed.")]
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
            Assert.AssemblyHasAttribute(result, Path.Combine(OutputPath, "AppWithP2PReference.dll"), "Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task Build_ProjectWithDependencyThatReferencesMvc_DoesNotGenerateAttributeIfFlagIsReset()
        {
            var result = await DotnetMSBuild("Build /p:GenerateMvcApplicationPartsAssemblyAttributes=false");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ProjectWithoutMvcReferencingDependencies_DoesNotGenerateAttribute()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cs");

            // We should produced a cache file for build incrementalism
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cache");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task BuildIncrementalism_WhenApplicationPartAttributeIsGenerated()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            var generatedAttributeFile = Path.Combine(IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs");
            var cacheFile = Path.Combine(IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cache");
            var outputFile = Path.Combine(IntermediateOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, generatedAttributeFile);
            Assert.FileContains(result, generatedAttributeFile, "[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute(\"ClassLibrary\")]");

            var generatedFilethumbPrint = GetThumbPrint(generatedAttributeFile);
            var cacheFileThumbPrint = GetThumbPrint(cacheFile);
            var outputFileThumbPrint = GetThumbPrint(outputFile);

            await AssertIncrementalBuild();
            await AssertIncrementalBuild();

            async Task AssertIncrementalBuild()
            {
                result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);

                Assert.FileExists(result, generatedAttributeFile);
                Assert.Equal(generatedFilethumbPrint, GetThumbPrint(generatedAttributeFile));
                Assert.Equal(cacheFileThumbPrint, GetThumbPrint(cacheFile));
                Assert.Equal(outputFileThumbPrint, GetThumbPrint(outputFile));
                Assert.AssemblyHasAttribute(result, Path.Combine(OutputPath, "AppWithP2PReference.dll"), "Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute");
            }
        }

        // Regression test for https://github.com/dotnet/aspnetcore/issues/11315
        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task BuildIncrementalism_CausingRecompilation_WhenApplicationPartAttributeIsGenerated()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            var generatedAttributeFile = Path.Combine(IntermediateOutputPath, "AppWithP2PReference.MvcApplicationPartsAssemblyInfo.cs");
            Assert.FileExists(result, generatedAttributeFile);
            Assert.FileContains(result, generatedAttributeFile, "[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute(\"ClassLibrary\")]");

            var thumbPrint = GetThumbPrint(generatedAttributeFile);

            // Touch a file in the main app which should call recompilation, but not the Mvc discovery tasks to re-run.
            File.AppendAllText(Path.Combine(Project.DirectoryPath, "Program.cs"), " ");
            result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, generatedAttributeFile);
            Assert.Equal(thumbPrint, GetThumbPrint(generatedAttributeFile));
            Assert.AssemblyHasAttribute(result, Path.Combine(OutputPath, "AppWithP2PReference.dll"), "Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute");
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/13303")]
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
            Assert.AssemblyHasAttribute(result, Path.Combine(OutputPath, "SimpleMvcFSharp.dll"), "Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildIncrementalism_WhenApplicationPartAttributeIsNotGenerated()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            var generatedAttributeFile = Path.Combine(IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cs");
            var cacheFile = Path.Combine(IntermediateOutputPath, "SimpleMvc.MvcApplicationPartsAssemblyInfo.cache");
            var outputFile = Path.Combine(IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, generatedAttributeFile);
            Assert.FileExists(result, cacheFile);

            var cacheFilethumbPrint = GetThumbPrint(cacheFile);
            var outputFilethumbPrint = GetThumbPrint(outputFile);

            // Couple rounds of incremental builds.
            await AssertIncrementalBuild();
            await AssertIncrementalBuild();
            await AssertIncrementalBuild();

            async Task AssertIncrementalBuild()
            {
                result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);

                Assert.FileDoesNotExist(result, generatedAttributeFile);
                Assert.FileExists(result, cacheFile);
                Assert.Equal(cacheFilethumbPrint, GetThumbPrint(cacheFile));
                Assert.Equal(outputFilethumbPrint, GetThumbPrint(outputFile));
            }
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task Build_ProjectWithMissingAssemblyReference_PrintsWarning()
        {
            var result = await DotnetMSBuild("Build /p:BuildProjectReferences=false");

            Assert.BuildFailed(result);

            Assert.BuildWarning(result, "RAZORSDK1007");
            Assert.BuildError(result, "CS0006");
        }
    }
}
