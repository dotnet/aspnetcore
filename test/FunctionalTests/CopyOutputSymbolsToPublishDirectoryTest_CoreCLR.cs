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
    public class CopyOutputSymbolsToPublishDirectoryTest_CoreCLR :
        LoggedTest, IClassFixture<CopyOutputSymbolsToPublishDirectoryTest_CoreCLR.TestFixture>
    {
        public CopyOutputSymbolsToPublishDirectoryTest_CoreCLR(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PublishingWithOption_SkipsPublishingPdb()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                var dllFile = Path.Combine(deployment.ContentRoot, "SimpleApp.PrecompiledViews.dll");
                var pdbFile = Path.ChangeExtension(dllFile, ".pdb");
                Assert.True(File.Exists(dllFile), $"{dllFile} does not exist at deployment.");
                Assert.False(File.Exists(pdbFile), $"{pdbFile} exists at deployment.");
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
                    new KeyValuePair<string, string>("CopyOutputSymbolsToPublishDirectory", "false"));

                return deploymentParameters;
            }
        }
    }
}
