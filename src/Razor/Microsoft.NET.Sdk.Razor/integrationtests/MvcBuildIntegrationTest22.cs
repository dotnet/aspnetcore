// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class MvcBuildIntegrationTest22 : MvcBuildIntegrationTestLegacy
    {
        public MvcBuildIntegrationTest22(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        public override string TestProjectName => "SimpleMvc22";
        public override string TargetFramework => "netcoreapp2.2";

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task BuildProject_UsingDesktopMSBuild()
        {
            using var _ = CreateTestProject();

            // Build
            // This is a regression test for https://github.com/dotnet/aspnetcore/issues/28333. We're trying to ensure
            // building in Desktop when DOTNET_HOST_PATH is not configured continues to work.
            // Explicitly unset it to verify a value is not being picked up as an ambient value.
            var result = await DotnetMSBuild("Build", args: "/p:DOTNET_HOST_PATH=", msBuildProcessKind: MSBuildProcessKind.Desktop);

            Assert.BuildPassed(result);
            Assert.FileExists(result, OutputPath, OutputFileName);
            Assert.FileExists(result, OutputPath, $"{TestProjectName}.pdb");
            Assert.FileExists(result, OutputPath, $"{TestProjectName}.Views.dll");
            Assert.FileExists(result, OutputPath, $"{TestProjectName}.Views.pdb");

            // Verify RazorTagHelper works
            Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.TagHelpers.input.cache");
            Assert.FileExists(result, IntermediateOutputPath, $"{TestProjectName}.TagHelpers.output.cache");
            Assert.FileContains(
                result,
                Path.Combine(IntermediateOutputPath, $"{TestProjectName}.TagHelpers.output.cache"),
                @"""Name"":""SimpleMvc.SimpleTagHelper""");
        }
    }
}
