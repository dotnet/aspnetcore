// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.Inprocess
{
    public class AppOfflineTests : IISFunctionalTestBase
    {
        // TODO these will differ between IIS and IISExpress
        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteIsDown_SiteReturns503()
        {
            var deploymentResult = await DeployApp();

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteIsDown_CustomResponse()
        {
            var expectedResponse = "The app is offline.";
            var deploymentResult = await DeployApp();

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot, expectedResponse);

            await AssertAppOffline(deploymentResult, expectedResponse);
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown()
        {
            var deploymentResult = await AssertStarts();

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertStopsProcess(deploymentResult);
        }

        [ConditionalFact]
        public async Task AppOfflineDropped_CanRemoveAppOfflineAfterAddingAndSiteWorks()
        {
            var deploymentResult = await DeployApp();

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);

            RemoveAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

        }

        private async Task<IISDeploymentResult> DeployApp()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters();

            return await DeployAsync(deploymentParameters);
        }

        private void AddAppOffline(string appPath, string content = "")
        {
            File.WriteAllText(Path.Combine(appPath, "app_offline.htm"), content);
        }

        private void RemoveAppOffline(string appPath)
        {
            RetryHelper.RetryOperation(
                () => File.Delete(Path.Combine(appPath, "app_offline.htm")),
                e => Logger.LogError($"Failed to remove app_offline : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: 100);
        }

        private async Task AssertAppOffline(IISDeploymentResult deploymentResult, string expectedResponse = "")
        {
            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            for (var i = 0; response.IsSuccessStatusCode && i < 5; i++)
            {
                // Keep retrying until app_offline is present.
                response = await deploymentResult.HttpClient.GetAsync("HelloWorld");
            }

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            Assert.Equal(expectedResponse, await response.Content.ReadAsStringAsync());
        }

        private async Task AssertStopsProcess(IISDeploymentResult deploymentResult)
        {
            try
            {
                var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");
            }
            catch (HttpRequestException)
            {
                // dropping app_offline will kill the process
            }

            var hostShutdownToken = deploymentResult.DeploymentResult.HostShutdownToken;

            Assert.True(hostShutdownToken.WaitHandle.WaitOne(Helpers.DefaultTimeout));
            Assert.True(hostShutdownToken.IsCancellationRequested);
        }

        private async Task<IISDeploymentResult> AssertStarts()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);

            return deploymentResult;
        }
    }
}
