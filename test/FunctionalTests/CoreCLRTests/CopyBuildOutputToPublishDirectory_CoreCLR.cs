// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class CopyBuildOutputToPublishDirectory_CoreCLR :
        LoggedTest, IClassFixture<CopyBuildOutputToPublishDirectory_CoreCLR.TestFixture>
    {
        public CopyBuildOutputToPublishDirectory_CoreCLR(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PublishingWithOption_SkipsPublishingPrecompiledDll()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                var dllFile = Path.Combine(deployment.ContentRoot, "SimpleApp.PrecompiledViews.dll");
                var pdbFile = Path.ChangeExtension(dllFile, ".pdb");
                Assert.False(File.Exists(dllFile), $"{dllFile} exists at deployment.");
                Assert.True(File.Exists(pdbFile), $"{pdbFile} does not exist at deployment.");
            }
        }

        public class TestFixture : CoreCLRApplicationTestFixture<SimpleApp.Startup>
        {
            public TestFixture()
            {
                PublishOnly = true;
            }

            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.PublishEnvironmentVariables.Add(
                    new KeyValuePair<string, string>("CopyBuildOutputToPublishDirectory", "false"));

                return deploymentParameters;
            }
        }
    }
}
