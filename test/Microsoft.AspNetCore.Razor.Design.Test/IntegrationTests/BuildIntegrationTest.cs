// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public Task Build_SimpleMvc_UsingDotnetMSBuildAndWithoutBuildServer_CanBuildSuccessfully()
            => Build_SimpleMvc_WithoutBuildServer_CanBuildSuccessfully(MSBuildProcessKind.Dotnet);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InitializeTestProject("SimpleMvc")]
        public Task Build_SimpleMvc_UsingDesktopMSBuildAndWithoutBuildServer_CanBuildSuccessfully()
            => Build_SimpleMvc_WithoutBuildServer_CanBuildSuccessfully(MSBuildProcessKind.Desktop);

        // This test is identical to the ones in BuildServerIntegrationTest except this one explicitly disables the Razor build server.
        private async Task Build_SimpleMvc_WithoutBuildServer_CanBuildSuccessfully(MSBuildProcessKind msBuildProcessKind)
        {
            var result = await DotnetMSBuild("Build",
                "/p:UseRazorBuildServer=false",
                suppressBuildServer: true,
                msBuildProcessKind: msBuildProcessKind);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // GetFullPath on OSX doesn't work well in travis. We end up computing a different path than will
                // end up in the MSBuild logs.
                Assert.BuildOutputContainsLine(result, $"SimpleMvc -> {Path.Combine(Path.GetFullPath(Project.DirectoryPath), OutputPath, "SimpleMvc.Views.dll")}");
            }
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_SimpleMvc_NoopsWithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_SimpleMvc_NoopsWithRazorCompileOnBuild_False()
        {
            var result = await DotnetMSBuild("Build", "/p:RazorCompileOnBuild=false");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_ErrorInGeneratedCode_ReportsMSBuildError()
        {
            // Introducing a C# semantic error
            ReplaceContent("@{ var foo = \"\".Substring(\"bleh\"); }", "Views", "Home", "Index.cshtml");

            var result = await DotnetMSBuild("Build");

            Assert.BuildFailed(result);

            // Verifying that the error correctly gets mapped to the original source
            Assert.BuildError(result, "CS1503", location: Path.Combine("Views", "Home", "Index.cshtml") + "(1,27)");

            // Compilation failed without creating the views assembly
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
        }

        [Fact]
        [InitializeTestProject("SimplePages")]
        public async Task Build_Works_WhenFilesAtDifferentPathsHaveSameNamespaceHierarchy()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimplePages.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimplePages.Views.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_RazorOutputPath_SetToNonDefault()
        {
            var customOutputPath = Path.Combine("bin", Configuration, TargetFramework, "Razor");
            var result = await DotnetMSBuild("Build", $"/p:RazorOutputPath={customOutputPath}");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, customOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, customOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_MvcRazorOutputPath_SetToNonDefault()
        {
            var customOutputPath = Path.Combine("bin", Configuration, TargetFramework, "Razor");
            var result = await DotnetMSBuild("Build", $"/p:MvcRazorOutputPath={customOutputPath}");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, customOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, customOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_SkipsCopyingBinariesToOutputDirectory_IfCopyBuildOutputToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Build", "/p:CopyBuildOutputToOutputDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_SkipsCopyingBinariesToOutputDirectory_IfCopyOutputSymbolsToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Build", "/p:CopyOutputSymbolsToOutputDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_Works_WhenSymbolsAreNotGenerated()
        {
            var result = await DotnetMSBuild("Build", "/p:DebugType=none");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", "ClassLibrary")]
        public async Task Build_WithP2P_CopiesRazorAssembly()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.Views.pdb");
            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimplePages", "LinkedDir")]
        public async Task Build_SetsUpEmbeddedResourcesWithLogicalName()
        {
            // Arrange
            var additionalProjectContent = @"
<ItemGroup>
  <Content Include=""..\LinkedDir\LinkedFile.cshtml"" Link=""LinkedFileOut\LinkedFile.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(additionalProjectContent);
            Directory.CreateDirectory(Path.Combine(Project.DirectoryPath, "..", "LinkedDir"));

            var result = await DotnetMSBuild("Build", "/t:_IntrospectRazorEmbeddedResources /p:EmbedRazorGenerateSources=true");

            Assert.BuildPassed(result);

            Assert.BuildOutputContainsLine(result, $@"CompileResource: {Path.Combine("Pages", "Index.cshtml")} /Pages/Index.cshtml");
            Assert.BuildOutputContainsLine(result, $@"CompileResource: {Path.Combine("Areas", "Products", "Pages", "_ViewStart.cshtml")} /Areas/Products/Pages/_ViewStart.cshtml");
            Assert.BuildOutputContainsLine(result, $@"CompileResource: {Path.Combine("..", "LinkedDir", "LinkedFile.cshtml")} /LinkedFileOut/LinkedFile.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_WithViews_ProducesDepsFileWithCompilationContext()
        {
            var customDefine = "RazorSdkTest";
            var result = await DotnetMSBuild("Build", $"/p:DefineConstants={customDefine}");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "SimpleMvc.deps.json");
            var depsFilePath = Path.Combine(Project.DirectoryPath, OutputPath, "SimpleMvc.deps.json");
            var dependencyContext = ReadDependencyContext(depsFilePath);
            // Pick a couple of libraries and ensure they have some compile references
            var packageReference = dependencyContext.CompileLibraries.First(l => l.Name == "Microsoft.AspNetCore.Html.Abstractions");
            Assert.NotEmpty(packageReference.Assemblies);

            var projectReference = dependencyContext.CompileLibraries.First(l => l.Name == "SimpleMvc");
            Assert.NotEmpty(packageReference.Assemblies);

            Assert.Contains(customDefine, dependencyContext.CompilationOptions.Defines);
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Build_WithoutViews_ProducesDepsFileWithotCompiilationContext()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);
            var customDefine = "RazorSdkTest";
            var result = await DotnetMSBuild("Build", $"/p:DefineConstants={customDefine}");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "SimpleMvc.deps.json");
            var depsFilePath = Path.Combine(Project.DirectoryPath, OutputPath, "SimpleMvc.deps.json");
            var dependencyContext = ReadDependencyContext(depsFilePath);
            Assert.All(dependencyContext.CompileLibraries, library => Assert.Empty(library.Assemblies));
            Assert.Empty(dependencyContext.CompilationOptions.Defines);
        }

        [Fact]
        [InitializeTestProject("ClassLibrary")]
        public async Task Build_ClassLibrary_DoesNotProduceDepsFile()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.deps.json");
        }

        private static DependencyContext ReadDependencyContext(string depsFilePath)
        {
            var reader = new DependencyContextJsonReader();
            using (var stream = File.OpenRead(depsFilePath))
            {
                return reader.Read(stream);
            }
        }
    }
}
