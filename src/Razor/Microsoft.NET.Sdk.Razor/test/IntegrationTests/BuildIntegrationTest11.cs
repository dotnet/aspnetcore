// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest11 : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildIntegrationTest11(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc11")]
        public async Task RazorSdk_DoesNotAddCoreRazorConfigurationTo11Projects()
        {
            var result = await DotnetMSBuild("_IntrospectProjectCapabilityItems");

            Assert.BuildPassed(result);
            Assert.BuildOutputContainsLine(result, "ProjectCapability: DotNetCoreRazor");
            Assert.BuildOutputDoesNotContainLine(result, "ProjectCapability: DotNetCoreRazorConfiguration");
        }

        [Fact]
        [InitializeTestProject("SimpleMvc11")]
        public async Task RazorSdk_DoesNotBuildViewsForNetCoreApp11Projects()
        {
            MSBuildIntegrationTestBase.TargetFramework = "netcoreapp1.1";
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc11.dll");
            Assert.FileExists(result, OutputPath, "SimpleMvc11.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc11.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc11.Views.pdb");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]

        [InitializeTestProject("SimpleMvc11NetFx")]
        public async Task RazorSdk_DoesNotBuildViewsForNetFx11Projects()
        {
            MSBuildIntegrationTestBase.TargetFramework = "net461";
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, "SimpleMvc11NetFx.exe");
            Assert.FileExists(result, OutputPath, "SimpleMvc11NetFx.pdb");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc11NetFx.Views.dll");
            Assert.FileDoesNotExist(result, OutputPath, "SimpleMvc11NetFx.Views.pdb");
        }
    }
}
