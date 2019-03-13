// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class AppOfflineTests : IISFunctionalTestBase
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

        private readonly PublishedSitesFixture _fixture;

        public AppOfflineTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineDroppedWhileSiteIsDown_SiteReturns503(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            AddAppOffline(deploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);
            DeletePublishOutput(deploymentResult);
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task LockedAppOfflineDroppedWhileSiteIsDown_SiteReturns503(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            // Add app_offline without shared access
            using (var stream = File.Open(Path.Combine(deploymentResult.ContentRoot, "app_offline.htm"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync("App if offline but you wouldn't see this message");
                await writer.FlushAsync();
                await AssertAppOffline(deploymentResult, "");
            }

            DeletePublishOutput(deploymentResult);
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess, 500, "500.0")]
        [InlineData(HostingModel.OutOfProcess, 502, "502.5")]
        public async Task AppOfflineDroppedWhileSiteFailedToStartInShim_AppOfflineServed(HostingModel hostingModel, int statusCode, string content)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel: hostingModel);
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "nonexistent"));

            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.GetAsync("/");
            Assert.Equal(statusCode, (int)result.StatusCode);
            Assert.Contains(content, await result.Content.ReadAsStringAsync());

            AddAppOffline(deploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);
            DeletePublishOutput(deploymentResult);
        }

        [ConditionalFact(Skip = "https://github.com/aspnet/IISIntegration/issues/933")]
        public async Task AppOfflineDroppedWhileSiteFailedToStartInRequestHandler_SiteStops_InProcess()
        {
            var deploymentResult = await DeployApp(HostingModel.InProcess);

            // Set file content to empty so it fails at runtime
            File.WriteAllText(Path.Combine(deploymentResult.ContentRoot, "Microsoft.AspNetCore.Server.IIS.dll"), "");

            var result = await deploymentResult.HttpClient.GetAsync("/");
            Assert.Equal(500, (int)result.StatusCode);
            Assert.Contains("500.30", await result.Content.ReadAsStringAsync());

            AddAppOffline(deploymentResult.ContentRoot);

            await deploymentResult.AssertRecycledAsync(() => AssertAppOffline(deploymentResult));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task AppOfflineDroppedWhileSiteStarting_SiteShutsDown_InProcess()
        {
            // This test often hits a race between debug logging and stdout redirection closing the handle
            // we are fine having this race
            using (AppVerifier.Disable(DeployerSelector.ServerType, 0x300))
            {
                var deploymentResult = await DeployApp(HostingModel.InProcess);

                for (int i = 0; i < 10; i++)
                {
                    // send first request and add app_offline while app is starting
                    var runningTask = AssertAppOffline(deploymentResult);

                    // This test tries to hit a race where we drop app_offline file while
                    // in process application is starting, application start takes at least 400ms
                    // so we back off for 100ms to allow request to reach request handler
                    // Test itself is racy and can result in two scenarios
                    //    1. ANCM detects app_offline before it starts the request - if AssertAppOffline succeeds we've hit it
                    //    2. Intended scenario where app starts and then shuts down
                    // In first case we remove app_offline and try again
                    await Task.Delay(RetryDelay);

                    AddAppOffline(deploymentResult.ContentRoot);

                    try
                    {
                        await runningTask.DefaultTimeout();

                        // if AssertAppOffline succeeded ANCM have picked up app_offline before starting the app
                        // try again
                        RemoveAppOffline(deploymentResult.ContentRoot);

                        if (deploymentResult.DeploymentParameters.ServerType == ServerType.IIS)
                        {
                            deploymentResult.AssertWorkerProcessStop();
                            return;
                        }
                    }
                    catch
                    {
                        // For IISExpress, we need to catch the exception because IISExpress will not restart a process if it crashed.
                        // RemoveAppOffline will fail due to a bad request exception as the server is down.
                        Assert.Contains(TestSink.Writes, context => context.Message.Contains("Drained all requests, notifying managed."));
                        deploymentResult.AssertWorkerProcessStop();
                        return;
                    }
                }

                Assert.True(false);
            }
        }

        [ConditionalFact]
        public async Task GracefulShutdownWorksWithMultipleRequestsInFlight_InProcess()
        {
            // The goal of this test is to have multiple requests currently in progress
            // and for app offline to be dropped. We expect that all requests are eventually drained
            // and graceful shutdown occurs. 
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.InProcessTestSite);
            deploymentParameters.TransformArguments((a, _) => $"{a} IncreaseShutdownLimit");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.GetAsync("/HelloWorld");

            // Send two requests that will hang until data is sent from the client.
            var connectionList = new List<TestConnection>();

            for (var i = 0; i < 2; i++)
            {
                var connection = new TestConnection(deploymentResult.HttpClient.BaseAddress.Port);
                await connection.Send(
                    "POST /ReadAndCountRequestBody HTTP/1.1",
                    "Content-Length: 1",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await connection.Receive(
                  "HTTP/1.1 200 OK", "");
                await connection.ReceiveHeaders();
                await connection.Receive("1", $"{i + 1}");
                connectionList.Add(connection);
            }

            // Send a request that will end once app lifetime is triggered (ApplicationStopping cts).
            var statusConnection = new TestConnection(deploymentResult.HttpClient.BaseAddress.Port);

            await statusConnection.Send(
                "GET /WaitForAppToStartShuttingDown HTTP/1.1",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await statusConnection.Receive("HTTP/1.1 200 OK",
                "");

            await statusConnection.ReceiveHeaders();

            // Receiving some data means we are currently waiting for IHostApplicationLifetime.
            await statusConnection.Receive("5",
                "test1",
                "");

            AddAppOffline(deploymentResult.ContentRoot);

            // Receive the rest of all open connections.
            await statusConnection.Receive("5", "test2", "");

            for (var i = 0; i < 2; i++)
            {
                await connectionList[i].Send("a", "");
                await connectionList[i].Receive("", "4", "done");
                connectionList[i].Dispose();
            }

            deploymentResult.AssertWorkerProcessStop();

            // Shutdown should be graceful here!
            EventLogHelpers.VerifyEventLogEvent(deploymentResult,
                EventLogHelpers.InProcessShutdown(), Logger);
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_InProcess()
        {
            var deploymentResult = await AssertStarts(HostingModel.InProcess);

            AddAppOffline(deploymentResult.ContentRoot);

            await deploymentResult.AssertRecycledAsync(() => AssertAppOffline(deploymentResult));
        }

        [ConditionalFact]
        public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_OutOfProcess()
        {
            var deploymentResult = await AssertStarts(HostingModel.OutOfProcess);

            // Repeat dropping file and restarting multiple times
            for (int i = 0; i < 5; i++)
            {
                AddAppOffline(deploymentResult.ContentRoot);
                await AssertAppOffline(deploymentResult);
                RemoveAppOffline(deploymentResult.ContentRoot);
                await AssertRunning(deploymentResult);
            }

            AddAppOffline(deploymentResult.ContentRoot);
            await AssertAppOffline(deploymentResult);
            DeletePublishOutput(deploymentResult);
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineDropped_CanRemoveAppOfflineAfterAddingAndSiteWorks(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            AddAppOffline(deploymentResult.ContentRoot);

            await AssertAppOffline(deploymentResult);

            RemoveAppOffline(deploymentResult.ContentRoot);

            await AssertRunning(deploymentResult);
        }

        [ConditionalTheory(Skip = "https://github.com/aspnet/AspNetCore/issues/7075")]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task AppOfflineAddedAndRemovedStress(HostingModel hostingModel)
        {
            var deploymentResult = await AssertStarts(hostingModel);

            var load = Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response => {
                var statusCode = (int)response.StatusCode;
                Assert.True(statusCode == 200 || statusCode == 503, "Status code was " + statusCode);
            });

            for (int i = 0; i < 100; i++)
            {
                // AddAppOffline might fail if app_offline is being read by ANCM and deleted at the same time
                RetryHelper.RetryOperation(
                    () => AddAppOffline(deploymentResult.ContentRoot),
                    e => Logger.LogError($"Failed to create app_offline : {e.Message}"),
                    retryCount: 3,
                    retryDelayMilliseconds: RetryDelay.Milliseconds);
                RemoveAppOffline(deploymentResult.ContentRoot);
            }

            try
            {
                await load;
            }
            catch (HttpRequestException ex) when (ex.InnerException is IOException | ex.InnerException is SocketException)
            {
                // IOException in InProcess is fine, just means process stopped
                if (hostingModel != HostingModel.InProcess)
                {
                    throw;
                }
            }
        }

        private async Task<IISDeploymentResult> DeployApp(HostingModel hostingModel = HostingModel.InProcess)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel: hostingModel);

            return await DeployAsync(deploymentParameters);
        }

        private void AddAppOffline(string appPath, string content = "The app is offline.")
        {
            File.WriteAllText(Path.Combine(appPath, "app_offline.htm"), content);
        }

        private void RemoveAppOffline(string appPath)
        {
            RetryHelper.RetryOperation(
                () => File.Delete(Path.Combine(appPath, "app_offline.htm")),
                e => Logger.LogError($"Failed to remove app_offline : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: RetryDelay.Milliseconds);
        }

        private async Task AssertAppOffline(IISDeploymentResult deploymentResult, string expectedResponse = "The app is offline.")
        {
            var response = await deploymentResult.HttpClient.RetryRequestAsync("HelloWorld", r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
            Assert.Equal(expectedResponse, await response.Content.ReadAsStringAsync());
        }

        private async Task<IISDeploymentResult> AssertStarts(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            await AssertRunning(deploymentResult);

            return deploymentResult;
        }

        private static async Task AssertRunning(IISDeploymentResult deploymentResult)
        {
            var response = await deploymentResult.HttpClient.RetryRequestAsync("HelloWorld", r => r.IsSuccessStatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);
        }

        private void DeletePublishOutput(IISDeploymentResult deploymentResult)
        {
            foreach (var file in Directory.GetFiles(deploymentResult.ContentRoot, "*", SearchOption.AllDirectories))
            {
                // Out of process module dll is allowed to be locked
                var name = Path.GetFileName(file);
                if (name == "aspnetcore.dll" || name == "aspnetcorev2.dll" || name == "aspnetcorev2_outofprocess.dll")
                {
                    continue;
                }
                File.Delete(file);
            }
        }
    }
}
