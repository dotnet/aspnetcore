// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class RazorCompileIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public RazorCompileIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorCompile_Success_CompilesAssembly()
        {
            var result = await DotnetMSBuild("RazorCompile");

            Assert.BuildPassed(result);

            // RazorGenerate should compile the assembly and pdb.
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorCompile_NoopsWithNoFiles()
        {
            Directory.Delete(Path.Combine(Project.DirectoryPath, "Views"), recursive: true);

            var result = await DotnetMSBuild("RazorCompile");

            Assert.BuildPassed(result);

            // Everything we do should noop - including building the app. 
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.dll");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")] 
        public async Task RazorCompile_EmbedRazorGenerateSources_EmbedsCshtmlFiles()
        {
            var result = await DotnetMSBuild("RazorCompile", "/p:EmbedRazorGenerateSources=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            var assembly = LoadAssemblyFromBytes(result.Project.DirectoryPath, IntermediateOutputPath, "SimpleMvc.Views.dll");
            var resources = assembly.GetManifestResourceNames();

            Assert.Equal(new string[]
            {
                "/Views/_ViewImports.cshtml",
                "/Views/_ViewStart.cshtml",
                "/Views/Home/About.cshtml",
                "/Views/Home/Contact.cshtml",
                "/Views/Home/Index.cshtml",
                "/Views/Shared/_Layout.cshtml",
                "/Views/Shared/_ValidationScriptsPartial.cshtml",
                "/Views/Shared/Error.cshtml",
            },
            resources.OrderBy(r => r));
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task RazorCompile_MvcRazorEmbedViewSources_EmbedsCshtmlFiles()
        {
            var result = await DotnetMSBuild("RazorCompile", "/p:EmbedRazorGenerateSources=true");

            Assert.BuildPassed(result);

            Assert.FileExists(result, IntermediateOutputPath, "SimpleMvc.Views.dll");

            var assembly = LoadAssemblyFromBytes(result.Project.DirectoryPath, IntermediateOutputPath, "SimpleMvc.Views.dll");
            var resources = assembly.GetManifestResourceNames();

            Assert.Equal(new string[]
            {
                "/Views/_ViewImports.cshtml",
                "/Views/_ViewStart.cshtml",
                "/Views/Home/About.cshtml",
                "/Views/Home/Contact.cshtml",
                "/Views/Home/Index.cshtml",
                "/Views/Shared/_Layout.cshtml",
                "/Views/Shared/_ValidationScriptsPartial.cshtml",
                "/Views/Shared/Error.cshtml",
            },
            resources.OrderBy(r => r));
        }

        private Assembly LoadAssemblyFromBytes(params string[] paths)
        {
            // We need to load the assembly from bytes to load it without locking the file - and yes, we need to
            // load the pdb too, or else the CLR will load/lock it based on the path specified in the assembly.
            var assemblyBytes = File.ReadAllBytes(Path.Combine(paths));
            var symbolBytes = File.ReadAllBytes(Path.ChangeExtension(Path.Combine(paths), ".pdb"));
            return Assembly.Load(assemblyBytes, symbolBytes);
        }
    }
}
