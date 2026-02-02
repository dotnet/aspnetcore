// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using System.Globalization;
using System.Diagnostics;
using System.IO;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
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

            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: HostingModel.OutOfProcess);
            var deployer = CreateDeployer(deploymentParameters);
            deployers.Add(deployer);

            // Deploy all apps
            for (var i = 0; i < numApps; i++)
            {
                deploymentResults.Add(await deployer.DeployAsync());
            }

            // Start all requests as quickly as possible, so apps are started as quickly as possible,
            // to test possible race conditions when multiple apps start at the same time.
            var requestTasks = new List<Task<HttpResponseMessage>>();
            foreach (var deploymentResult in deploymentResults)
            {
                requestTasks.Add(deploymentResult.HttpClient.RetryRequestAsync("/HelloWorld", r => r.IsSuccessStatusCode));
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

    [ConditionalFact]
    public async Task Restart()
    {
        const int numApps = 10;

        using (var deployers = new DisposableList<ApplicationDeployer>())
        {
            var deploymentResults = new List<DeploymentResult>();

            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: HostingModel.OutOfProcess);
            var deployer = CreateDeployer(deploymentParameters);
            deployers.Add(deployer);

            // Deploy all apps
            for (var i = 0; i < numApps; i++)
            {
                deploymentResults.Add(await deployer.DeployAsync());
            }

            // Start all requests as quickly as possible, so apps are started as quickly as possible,
            // to test possible race conditions when multiple apps start at the same time.
            var requestTasks = new List<Task<HttpResponseMessage>>();
            foreach (var deploymentResult in deploymentResults)
            {
                requestTasks.Add(deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", r => r.IsSuccessStatusCode));
            }

            List<int> processIDs = new();
            // Verify all apps started and return expected response
            foreach (var requestTask in requestTasks)
            {
                var response = await requestTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseText = await response.Content.ReadAsStringAsync();
                processIDs.Add(int.Parse(responseText, CultureInfo.InvariantCulture));
            }

            // Just "touching" web.config should be enough to restart the process
            deploymentResults[0].ModifyWebConfig(_ => { });

            // Need to give time for process to start and finish restarting
            await deploymentResults[0].HttpClient.RetryRequestAsync("/ProcessId",
                async r => int.Parse(await r.Content.ReadAsStringAsync(), CultureInfo.InvariantCulture) != processIDs[0]);

            // First process should have restarted
            var res = await deploymentResults[0].HttpClient.RetryRequestAsync("/ProcessId", r => r.IsSuccessStatusCode);

            Assert.NotEqual(processIDs[0], int.Parse(await res.Content.ReadAsStringAsync(), CultureInfo.InvariantCulture));

            // Every other process should stay the same
            for (var i = 1; i < deploymentResults.Count; i++)
            {
                res = await deploymentResults[i].HttpClient.GetAsync("/ProcessId");
                Assert.Equal(processIDs[i], int.Parse(await res.Content.ReadAsStringAsync(), CultureInfo.InvariantCulture));
            }
        }
    }
}
