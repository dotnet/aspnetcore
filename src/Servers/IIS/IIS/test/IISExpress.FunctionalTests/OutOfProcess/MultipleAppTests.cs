// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class MultipleAppTests : IISFunctionalTestBase
    {
        public MultipleAppTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        public async Task Startup()
        {
            const int numApps = 10;

            using (var deployers = new DisposableList<ApplicationDeployer>())
            {
                var deploymentResults = new List<DeploymentResult>();

                // Deploy all apps
                for (var i = 0; i < numApps; i++)
                {
                    var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: IntegrationTesting.HostingModel.OutOfProcess);
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
        }
    }
}
