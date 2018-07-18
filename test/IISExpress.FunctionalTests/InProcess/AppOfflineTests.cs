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
        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineDroppedWhileSiteIsDown_SiteReturns503(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineDroppedWhileSiteIsDown_CustomResponse(HostingModel hostingModel)
        {
            var expectedResponse = "The app is offline.";
            var deploymentResult = await DeployApp(hostingModel);

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot, expectedResponse);

            await AssertAppOffline(deploymentResult, expectedResponse);
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_InProcess()
        {
            var deploymentResult = await AssertStarts(HostingModel.InProcess);

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertStopsProcess(deploymentResult);
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_OutOfProcess()
        {
            var deploymentResult = await AssertStarts(HostingModel.OutOfProcess);

            // Repeat dropping file and restarting multiple times
            for (int i = 0; i < 5; i++)
            {
                AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);
                await AssertAppOffline(deploymentResult);
                RemoveAppOffline(deploymentResult.DeploymentResult.ContentRoot);
                await AssertRunning(deploymentResult);
            }
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineDropped_CanRemoveAppOfflineAfterAddingAndSiteWorks(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            AddAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);

            RemoveAppOffline(deploymentResult.DeploymentResult.ContentRoot);

            await AssertRunning(deploymentResult);
        }

        private async Task<IISDeploymentResult> DeployApp(HostingModel hostingModel = HostingModel.InProcess)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(hostingModel: hostingModel, publish: true);

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

            Assert.True(hostShutdownToken.WaitHandle.WaitOne(TimeoutExtensions.DefaultTimeout));
            Assert.True(hostShutdownToken.IsCancellationRequested);
        }

        private async Task<IISDeploymentResult> AssertStarts(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            await AssertRunning(deploymentResult);

            return deploymentResult;
        }

        private static async Task AssertRunning(IISDeploymentResult deploymentResult)
        {
            var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);
        }
    }
}
