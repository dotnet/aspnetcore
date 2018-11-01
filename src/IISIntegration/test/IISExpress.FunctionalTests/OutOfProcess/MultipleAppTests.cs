// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class MultipleAppTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public MultipleAppTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData(AncmVersion.AspNetCoreModule)]
        [InlineData(AncmVersion.AspNetCoreModuleV2)]
        public Task Startup(AncmVersion ancmVersion)
        {
            // ANCM v1 currently does *not* retry if an app fails to start the first time due to a port collision.
            // So, this test is expected to fail on v1 approximately 1 in 1,000 times (probably of at least one collision
            // when 10 sites choose a random port from the range 1025-48000).  Adding one retry should reduce the failure
            // rate from 1 in 1,000 to 1 in 1,000,000.  The previous product code (with "srand(GetTickCount())") should still
            // fail the test reliably.
            // https://github.com/aspnet/IISIntegration/issues/1350
            // 
            // ANCM v2 does retry on port collisions, so no retries should be required.
            var attempts = (ancmVersion == AncmVersion.AspNetCoreModule) ? 2 : 1;

            return Helpers.Retry(async () =>
            {
                const int numApps = 10;

                using (var deployers = new DisposableList<ApplicationDeployer>())
                {
                    var deploymentResults = new List<DeploymentResult>();

                    // Deploy all apps
                    for (var i = 0; i < numApps; i++)
                    {
                        var deploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel: IntegrationTesting.HostingModel.OutOfProcess);
                        deploymentParameters.AncmVersion = ancmVersion;

                        var deployer = CreateDeployer(deploymentParameters);
                        deployers.Add(deployer);
                        deploymentResults.Add(await deployer.DeployAsync());
                    }

                    // Start all requests as quickly as possible, so apps are started as quickly as possible,
                    // to test possible race conditions when multiple apps start at the same time.
                    var requestTasks = new List<Task<HttpResponseMessage>>();
                    foreach (var deploymentResult in deploymentResults)
                    {
                        requestTasks.Add(deploymentResult.HttpClient.GetAsync("/HelloWorld"));
                    }

                    // Verify all apps started and return expected response
                    foreach (var requestTask in requestTasks)
                    {
                        var response = await requestTask;
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        var responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("Hello World", responseText);
                    }
                }
            },
            attempts: attempts, msDelay: 0);
        }
    }
}
