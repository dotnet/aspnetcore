// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class PublishIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public PublishIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_RazorCompileOnPublish_IsDefault()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // Verify assets get published
            Assert.FileExists(result, PublishOutputPath, "wwwroot", "js", "SimpleMvc.js");
            Assert.FileExists(result, PublishOutputPath, "wwwroot", "css", "site.css");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_PublishesAssembly()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithRazorCompileOnPublish_PublishesAssembly()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithRazorCompileOnBuildFalse_PublishesAssembly()
        {
            // RazorCompileOnBuild is turned off, but RazorCompileOnPublish should still be enabled
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=false");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact] // This will use the old precompilation tool, RazorSDK shouldn't get involved.
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithMvcRazorCompileOnPublish_Noops()
        {
            var result = await DotnetMSBuild("Publish", "/p:MvcRazorCompileOnPublish=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact] // This is an override to force the new toolset
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithMvcRazorCompileOnPublish_AndRazorSDK_PublishesAssembly()
        {
            var result = await DotnetMSBuild("Publish", "/p:MvcRazorCompileOnPublish=true /p:ResolvedRazorCompileToolset=RazorSDK");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_NoopsWithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_NoopsWithMvcRazorCompileOnPublish_False()
        {
            var result = await DotnetMSBuild("Publish", "/p:MvcRazorCompileOnPublish=false");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_NoopsWith_RazorCompileOnPublishFalse()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnPublish=false");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_SkipsCopyingBinariesToOutputDirectory_IfCopyBuildOutputToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Publish", "/p:CopyBuildOutputToPublishDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_SkipsCopyingBinariesToOutputDirectory_IfCopyOutputSymbolsToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Publish", "/p:CopyOutputSymbolsToPublishDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_Works_WhenSymbolsAreNotGenerated()
        {
            var result = await DotnetMSBuild("Publish", "/p:DebugType=none");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_IncludeCshtmlAndRefAssemblies_CopiesFiles()
        {
            var result = await DotnetMSBuild("Publish", "/p:CopyRazorGenerateFilesToPublishDirectory=true /p:CopyRefAssembliesToPublishDirectory=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileExists(result, PublishOutputPath, "refs", "mscorlib.dll");
            Assert.FileCountEquals(result, 8, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact] // Tests old MvcPrecompilation behavior that we support for compat.
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_MvcRazorExcludeFilesFromPublish_False_CopiesFiles()
        {
            var result = await DotnetMSBuild("Publish", "/p:MvcRazorExcludeViewFilesFromPublish=false /p:MvcRazorExcludeRefAssembliesFromPublish=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileExists(result, PublishOutputPath, "refs", "mscorlib.dll");
            Assert.FileCountEquals(result, 8, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task Publish_WithP2P_AndRazorCompileOnBuild_CopiesRazorAssembly()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: "ClassLibrary")]
        public async Task Publish_WithP2P_AndRazorCompileOnPublish_CopiesRazorAssembly()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.pdb");

            // Verify fix for https://github.com/aspnet/Razor/issues/2295. No cshtml files should be published from the app
            // or the ClassLibrary.
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");
        }

        [Fact]
        [InitializeTestProject("SimpleMvcFSharp", language: "F#")]
        public async Task Publish_SimpleMvcFSharp_NoopsWithoutFailing()
        {
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvcFSharp.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvcFSharp.pdb");

            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvcFSharp.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvcFSharp.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_DoesNotPublishCustomRazorGenerateItems()
        {
            var additionalProjectContent = @"
<PropertyGroup>
    <EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
</PropertyGroup>
<ItemGroup>
  <RazorGenerate Include=""Views\_ViewImports.cshtml"" />
  <RazorGenerate Include=""Views\Home\Index.cshtml"" />
</ItemGroup>
";
            AddProjectFileContent(additionalProjectContent);
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.pdb");

            // Verify assets get published
            Assert.FileExists(result, PublishOutputPath, "wwwroot", "js", "SimpleMvc.js");
            Assert.FileExists(result, PublishOutputPath, "wwwroot", "css", "site.css");

            // By default refs and .cshtml files will not be copied on publish
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
            // Custom RazorGenerate item does not get published
            Assert.FileDoesNotExist(result, PublishOutputPath, "Views", "Home", "Home.cshtml");
            // cshtml Content item that's not part of RazorGenerate gets published.
            Assert.FileExists(result, PublishOutputPath, "Views", "Home", "About.cshtml");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", additionalProjects: new[] { "ClassLibrary", "ClassLibrary2" })]
        public async Task Publish_WithP2P_WorksWhenBuildProjectReferencesIsDisabled()
        {
            // Simulates publishing the same way VS does by setting BuildProjectReferences=false.
            // With this flag, P2P references aren't resolved during GetCopyToPublishDirectoryItems which would cause
            // any target that uses References as inputs to not be incremental. This test verifies no Razor Sdk work
            // is performed at this time.
            var additionalProjectContent = @"
<ItemGroup>
  <ProjectReference Include=""..\ClassLibrary2\ClassLibrary2.csproj"" />
</ItemGroup>
";
            AddProjectFileContent(additionalProjectContent);

            var result = await DotnetMSBuild(target: default);

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, OutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary2.dll");
            Assert.FileExists(result, OutputPath, "ClassLibrary2.Views.dll");

            // dotnet msbuild /t:Publish /p:BuildProjectReferences=false
            result = await DotnetMSBuild(target: "Publish", "/p:BuildProjectReferences=false", suppressRestore: true);

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.Views.pdb");

            Assert.FileExists(result, PublishOutputPath, "ClassLibrary2.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary2.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary2.Views.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary2.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithNoBuild_FailsWithoutBuild()
        {
            // Publish without building shouldn't succeed.
            var result = await DotnetMSBuild("Publish", "/p:NoBuild=true");

            Assert.BuildFailed(result);
            Assert.BuildError(result, "MSB3030"); // Could not copy the file "obj/Debug/netcoreapp2.2/SimpleMvc.dll because it couldn't be found.

            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.Views.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithNoBuild_CopiesAlreadyCompiledViews()
        {
            // Build
            var result = await DotnetMSBuild("Build", "/p:AssemblyVersion=1.1.1.1");

            Assert.BuildPassed(result);
            var assemblyPath = Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            var viewsAssemblyPath = Assert.FileExists(result, OutputPath, "SimpleMvc.Views.dll");
            var assemblyVersion = AssemblyName.GetAssemblyName(assemblyPath).Version;
            var viewsAssemblyVersion = AssemblyName.GetAssemblyName(viewsAssemblyPath).Version;

            // Publish should copy dlls from OutputPath
            result = await DotnetMSBuild("Publish", "/p:NoBuild=true");

            Assert.BuildPassed(result);
            var publishAssemblyPath = Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            var publishViewsAssemblyPath = Assert.FileExists(result, PublishOutputPath, "SimpleMvc.Views.dll");
            var publishAssemblyVersion = AssemblyName.GetAssemblyName(publishAssemblyPath).Version;
            var publishViewsAssemblyVersion = AssemblyName.GetAssemblyName(publishViewsAssemblyPath).Version;

            Assert.Equal(assemblyVersion, publishAssemblyVersion);
            Assert.Equal(viewsAssemblyVersion, publishViewsAssemblyVersion);
        }
    }
}
