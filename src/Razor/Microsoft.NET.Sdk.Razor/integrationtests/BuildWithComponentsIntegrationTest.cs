// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildWithComponentsIntegrationTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildWithComponentsIntegrationTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("MvcWithComponents")]
        public Task Build_Components_WithDotNetCoreMSBuild_Works() => Build_ComponentsWorks(MSBuildProcessKind.Dotnet);

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject("MvcWithComponents")]
        public Task Build_Components_WithDesktopMSBuild_Works() => Build_ComponentsWorks(MSBuildProcessKind.Desktop);

        [Fact]
        [InitializeTestProject("ComponentApp")]
        public async Task Build_DoesNotProduceMvcArtifacts_IfProjectDoesNotContainRazorGenerateItems()
        {
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ComponentApp.dll");
            Assert.FileExists(result, OutputPath, "ComponentApp.pdb");

            // Verify component compilation succeeded
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "ComponentApp.dll"), "ComponentApp.Components.Pages.Counter");

            // Verify MVC artifacts do not appear in the output.
            Assert.FileDoesNotExist(result, OutputPath, "ComponentApp.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ComponentApp.Views.pdb");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.RazorAssemblyInfo.cs");
            Assert.FileDoesNotExist(result, IntermediateOutputPath, "SimpleMvc.RazorTargetAssemblyInfo.cs");
        }

        [Fact]
        [InitializeTestProject("ComponentApp")]
        public async Task Build_Successful_WhenThereAreWarnings()
        {
            ReplaceContent("<UnrecognizedComponent />", "Components", "Pages", "Index.razor");
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result, allowWarnings: true);

            Assert.FileExists(result, OutputPath, "ComponentApp.dll");
            Assert.FileExists(result, OutputPath, "ComponentApp.pdb");

            // Verify component compilation succeeded
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "ComponentApp.dll"), "ComponentApp.Components.Pages.Counter");

            Assert.BuildWarning(result, "RZ10012");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task Build_WithoutRazorLangVersion_ProducesWarning()
        {
            Project.TargetFramework = "netstandard2.0";
            var result = await DotnetMSBuild("Build", "/p:RazorLangVersion=");

            Assert.BuildPassed(result, allowWarnings: true);

            // We should see a build warning
            Assert.BuildWarning(result, "RAZORSDK1005");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task Building_NetstandardComponentLibrary()
        {
            Project.TargetFramework = "netstandard2.0";

            // Build
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "ComponentLibrary.dll");
            Assert.FileExists(result, OutputPath, "ComponentLibrary.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "ComponentLibrary.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "ComponentLibrary.Views.pdb");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task Build_DoesNotProduceRefsDirectory()
        {
            Project.TargetFramework = "netstandard2.0";

            // Build
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);

            Assert.FileExists(result, OutputPath, "ComponentLibrary.dll");
           Assert.FileCountEquals(result, 0, Path.Combine(OutputPath, "refs"), "*.dll");
        }

        [Fact]
        [InitializeTestProject("ComponentLibrary")]
        public async Task Publish_DoesNotProduceRefsDirectory()
        {
            Project.TargetFramework = "netstandard2.0";

            // Build
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);

            Assert.FileExists(result, PublishOutputPath, "ComponentLibrary.dll");
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
        }

        private async Task Build_ComponentsWorks(MSBuildProcessKind msBuildProcessKind)
        {
            var result = await DotnetMSBuild("Build", msBuildProcessKind: msBuildProcessKind);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "MvcWithComponents.dll");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.pdb");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.Views.dll");
            Assert.FileExists(result, OutputPath, "MvcWithComponents.Views.pdb");

            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "MvcWithComponents.dll"), "MvcWithComponents.TestComponent");
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "MvcWithComponents.dll"), "MvcWithComponents.Views.Shared.NavMenu");

            // This is a component file with a .cshtml extension. It should appear in the main assembly, but not in the views dll.
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "MvcWithComponents.dll"), "MvcWithComponents.Components.Counter");
            Assert.AssemblyDoesNotContainType(result, Path.Combine(OutputPath, "MvcWithComponents.Views.dll"), "MvcWithComponents.Components.Counter");
            Assert.AssemblyDoesNotContainType(result, Path.Combine(OutputPath, "MvcWithComponents.Views.dll"), "AspNetCore.Components_Counter");

            // Verify a regular View appears in the views dll, but not in the main assembly.
            Assert.AssemblyDoesNotContainType(result, Path.Combine(OutputPath, "MvcWithComponents.dll"), "AspNetCore.Views.Home.Index");
            Assert.AssemblyDoesNotContainType(result, Path.Combine(OutputPath, "MvcWithComponents.dll"), "AspNetCore.Views_Home_Index");
            Assert.AssemblyContainsType(result, Path.Combine(OutputPath, "MvcWithComponents.Views.dll"), "AspNetCore.Views_Home_Index");
        }
    }
}
