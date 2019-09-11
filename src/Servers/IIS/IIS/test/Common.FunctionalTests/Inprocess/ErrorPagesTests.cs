// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ErrorPagesTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public ErrorPagesTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task IncludesAdditionalErrorPageTextInProcessHandlerLoadFailure_CorrectString()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var response = await DeployAppWithStartupFailure(deploymentParameters);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("HTTP Error 500.0 - ANCM In-Process Handler Load Failure", responseString);
            VerifyNoExtraTrailingBytes(responseString);

            await AssertLink(response);
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task IncludesAdditionalErrorPageTextOutOfProcessStartupFailure_CorrectString()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess, publish: true);
            var response = await DeployAppWithStartupFailure(deploymentParameters);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            StopServer();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("HTTP Error 502.5 - ANCM Out-Of-Process Startup Failure", responseString);
            VerifyNoExtraTrailingBytes(responseString);

            await AssertLink(response);
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task IncludesAdditionalErrorPageTextOutOfProcessHandlerLoadFailure_CorrectString()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess, publish: true);
            deploymentParameters.HandlerSettings["handlerVersion"] = "88.93";
            deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("HTTP Error 500.0 - ANCM Out-Of-Process Handler Load Failure", responseString);
            VerifyNoExtraTrailingBytes(responseString);

            await AssertLink(response);
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [RequiresNewHandler]
        public async Task IncludesAdditionalErrorPageTextInProcessStartupFailure_CorrectString()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.TransformArguments((a, _) => $"{a} EarlyReturn");
            deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("HTTP Error 500.30 - ANCM In-Process Start Failure", responseString);
            VerifyNoExtraTrailingBytes(responseString);

            await AssertLink(response);
        }

        private static void VerifyNoExtraTrailingBytes(string responseString)
        {
            if (DeployerSelector.HasNewShim)
            {
                Assert.EndsWith("</html>\r\n", responseString);
            }
        }

        private static async Task AssertLink(HttpResponseMessage response)
        {
            Assert.Contains("<a href=\"http://example\"> <cite> http://example </cite></a> and ", await response.Content.ReadAsStringAsync());
        }

        private async Task<HttpResponseMessage> DeployAppWithStartupFailure(IISDeploymentParameters deploymentParameters)
        {
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "doesnot"));
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("arguments", "start"));

            deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

            var deploymentResult = await DeployAsync(deploymentParameters);

            return await deploymentResult.HttpClient.GetAsync("HelloWorld");
        }
    }
}
