// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public class BuildIntegrationTest22NetFx : BuildIntegrationTestLegacy
    {
        public BuildIntegrationTest22NetFx(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        public override string TestProjectName => "SimpleMvc22NetFx";
        public override string TargetFramework => "net461";
        public override string OutputFileName => $"{TestProjectName}.exe";

        [Fact]
        public override async Task BuildingProject_CopyToOutputDirectoryFiles()
        {
            using (CreateTestProject())
            {
                // Build
                var result = await DotnetMSBuild("Build");

                Assert.BuildPassed(result);
                // No cshtml files should be in the build output directory
                Assert.FileCountEquals(result, 0, Path.Combine(OutputPath, "Views"), "*.cshtml");

                // refs are required for runtime compilation in desktop targeting projects.
                Assert.FileCountEquals(result, 97, Path.Combine(OutputPath, "refs"), "*.dll");
            }
        }
    }
}
