// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class PublishIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithRazorCompileOnBuild_PublishesAssembly()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, OutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc.PrecompiledViews.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_WithRazorCompileOnPublish_PublishesAssembly()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnPublish=true");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc.PrecompiledViews.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_NoopsWithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_SkipsCopyingBinariesToOutputDirectory_IfCopyBuildOutputToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true /p:CopyBuildOutputToPublishDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.dll");

            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_SkipsCopyingBinariesToOutputDirectory_IfCopyOutputSymbolsToOutputDirectory_IsUnset()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true /p:CopyOutputSymbolsToPublishDirectory=false");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task Publish_Works_WhenSymbolsAreNotGenerated()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true /p:DebugType=none");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.PrecompiledViews.pdb");

            Assert.FileExists(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, PublishOutputPath, "SimpleMvc.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", "ClassLibrary")]
        public async Task Publish_WithP2P_AndRazorCompileOnBuild_CopiesRazorAssembly()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.PrecompiledViews.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.PrecompiledViews.pdb");
        }

        [Fact]
        [InitializeTestProject("AppWithP2PReference", "ClassLibrary")]
        public async Task Publish_WithP2P_AndRazorCompileOnPublish_CopiesRazorAssembly()
        {
            var result = await DotnetMSBuild("Publish", "/p:RazorCompileOnPublish=true");

            Assert.BuildPassed(result);

            Assert.FileDoesNotExist(result, OutputPath, "AppWithP2PReference.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, OutputPath, "AppWithP2PReference.PrecompiledViews.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.PrecompiledViews.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ClassLibrary.PrecompiledViews.pdb");

            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.pdb");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "AppWithP2PReference.PrecompiledViews.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.pdb");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.PrecompiledViews.dll");
            Assert.FileExists(result, PublishOutputPath, "ClassLibrary.PrecompiledViews.pdb");
        }
    }
}
