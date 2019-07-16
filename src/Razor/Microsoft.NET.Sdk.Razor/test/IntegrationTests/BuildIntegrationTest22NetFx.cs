// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildIntegrationTest22NetFx :
        MSBuildIntegrationTestBase,
        IClassFixture<LegacyBuildServerTestFixture>
    {
        private const string TestProjectName = "SimpleMvc22NetFx";

        public BuildIntegrationTest22NetFx(LegacyBuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        public string OutputFileName => $"{TestProjectName}.exe";

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject(TestProjectName)]
        public async Task BuildingProject_CopyToOutputDirectoryFiles()
        {
            TargetFramework = "net461";

            // Build
            var result = await DotnetMSBuild("Build");

            Assert.BuildPassed(result);
            // No cshtml files should be in the build output directory
            Assert.FileCountEquals(result, 0, Path.Combine(OutputPath, "Views"), "*.cshtml");

            // refs are required for runtime compilation in desktop targeting projects.
            Assert.FileCountEquals(result, 97, Path.Combine(OutputPath, "refs"), "*.dll");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject(TestProjectName)]
        public async Task PublishingProject_CopyToOutputDirectoryFiles()
        {
            TargetFramework = "net461";

            // Build
            var result = await DotnetMSBuild("Publish");

            Assert.BuildPassed(result);
            // No cshtml files should be in the build output directory
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "Views"), "*.cshtml");

            // refs shouldn't be produced by default
            Assert.FileCountEquals(result, 0, Path.Combine(PublishOutputPath, "refs"), "*.dll");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InitializeTestProject(TestProjectName)]
        public async Task PublishingProject_CopyToOutputDirectoryFiles_WithCopyRefAssembliesToPublishDirectory()
        {
            TargetFramework = "net461";

            // Build
            var result = await DotnetMSBuild("Publish", "/p:CopyRefAssembliesToPublishDirectory=true");

            Assert.BuildPassed(result);

            // refs should be present if CopyRefAssembliesToPublishDirectory is set. 
            Assert.FileExists(result, PublishOutputPath, "refs", "System.Threading.Tasks.Extensions.dll");
        }
    }
}
