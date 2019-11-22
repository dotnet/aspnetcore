// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(PublishedSitesCollection.Name)]
    public class MaxRequestBodySizeTests : IISFunctionalTestBase
    {
        public MaxRequestBodySizeTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task MaxRequestBodySizeE2EWorks()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.TransformArguments((a, _) => $"{a} DecreaseRequestLimit");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.PostAsync("/ReadRequestBody", new StringContent("test"));
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, result.StatusCode);
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task SetIISLimitMaxRequestBodySizeE2EWorks()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.ServerConfigActionList.Add(
                (config, _) => {
                    config
                        .RequiredElement("system.webServer")
                        .GetOrAdd("security")
                        .GetOrAdd("requestFiltering")
                        .GetOrAdd("requestLimits", "maxAllowedContentLength", "1");
                });
            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.PostAsync("/ReadRequestBody", new StringContent("test"));

            // IIS either returns a 404 or a 413 based on versions of IIS.
            // Check for both as we don't know which specific patch version.
            Assert.True(result.StatusCode == HttpStatusCode.NotFound || result.StatusCode == HttpStatusCode.RequestEntityTooLarge);
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task IISRejectsContentLengthTooLargeByDefault()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            using (var connection = new TestConnection(deploymentResult.HttpClient.BaseAddress.Port))
            {
                await connection.Send(
                    "POST /HelloWorld HTTP/1.1",
                    $"Content-Length: 30000001",
                    "Host: localhost",
                    "",
                    "A");
                var requestLine = await connection.ReadLineAsync();
                Assert.True(requestLine.Contains("404") || requestLine.Contains("413"));
            }
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task SetIISLimitMaxRequestBodyLogsWarning()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();

            // Logs get tangled up due to ANCM debug logs and managed logs logging at the same time.
            // Disable it for this test as we are trying to verify a log.
            deploymentParameters.HandlerSettings["debugLevel"] = "";
            deploymentParameters.ServerConfigActionList.Add(
                (config, _) => {
                    config
                        .RequiredElement("system.webServer")
                        .GetOrAdd("security")
                        .GetOrAdd("requestFiltering")
                        .GetOrAdd("requestLimits", "maxAllowedContentLength", "1");
                });
            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.PostAsync("/DecreaseRequestLimit", new StringContent("1"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            StopServer();

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                Assert.Single(TestSink.Writes, w => w.Message.Contains("Increasing the MaxRequestBodySize conflicts with the max value for IIS limit maxAllowedContentLength." +
                    " HTTP requests that have a content length greater than maxAllowedContentLength will still be rejected by IIS." +
                    " You can disable the limit by either removing or setting the maxAllowedContentLength value to a higher limit."));
            }
        }
    }
}
