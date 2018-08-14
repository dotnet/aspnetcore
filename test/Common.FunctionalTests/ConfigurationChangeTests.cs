// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ConfigurationChangeTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public ConfigurationChangeTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ConfigurationChangeStopsInProcess()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(HostingModel.InProcess, publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.AssertStarts();

            // Just "touching" web.config should be enough
            deploymentResult.ModifyWebConfig(element => {});

            await deploymentResult.AssertRecycledAsync();
        }

        [ConditionalTheory]
        [InlineData(AncmVersion.AspNetCoreModule)]
        [InlineData(AncmVersion.AspNetCoreModuleV2)]
        public async Task ConfigurationChangeForcesChildProcessRestart(AncmVersion version)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess, publish: true);
            deploymentParameters.AncmVersion = version;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var processBefore = await deploymentResult.HttpClient.GetStringAsync("/ProcessId");

            // Just "touching" web.config should be enough
            deploymentResult.ModifyWebConfig(element => {});

            // Have to retry here to allow ANCM to receive notification and react to it
            // Verify that worker process gets restarted with new process id
            await deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", async r => await r.Content.ReadAsStringAsync() != processBefore);
        }
    }
}
