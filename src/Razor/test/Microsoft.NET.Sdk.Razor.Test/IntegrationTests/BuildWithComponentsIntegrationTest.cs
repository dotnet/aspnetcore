// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
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
        }
    }
}
