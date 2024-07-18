// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

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

// Contains all tests related to shutdown, including app_offline, abort, and app recycle
[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ShutdownTests : IISFunctionalTestBase
{
    public ShutdownTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52676")]
    public async Task ShutdownTimeoutIsApplied()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} HangOnStop");
        deploymentParameters.WebConfigActionList.Add(
            WebConfigHelpers.AddOrModifyAspNetCoreSection("shutdownTimeLimit", "1"));

        var deploymentResult = await DeployAsync(deploymentParameters);

        Assert.Equal("Hello World", await deploymentResult.HttpClient.GetStringAsync("/HelloWorld"));

        StopServer();

        EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.InProcessStarted(deploymentResult),
            EventLogHelpers.InProcessFailedToStop(deploymentResult, ""));
    }

    [ConditionalTheory]
    [InlineData("/ShutdownStopAsync")]
    [InlineData("/ShutdownStopAsyncWithCancelledToken")]
    public async Task CallStopAsyncOnRequestThread_DoesNotHangIndefinitely(string path)
    {
        // Canceled token doesn't affect shutdown, in-proc doesn't handle ungraceful shutdown
        // IIS's ShutdownTimeLimit will handle that.
        var parameters = Fixture.GetBaseDeploymentParameters();
        var deploymentResult = await DeployAsync(parameters);
        try
        {
            await deploymentResult.HttpClient.GetAsync(path);
        }
        catch (HttpRequestException ex) when (ex.InnerException is IOException)
        {
            // Server might close a connection before request completes
        }

        deploymentResult.AssertWorkerProcessStop();
    }

    [ConditionalFact]
    public async Task AppOfflineDroppedWhileSiteIsDown_SiteReturns503_InProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.InProcess);

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);
        DeletePublishOutput(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task AppOfflineDroppedWhileSiteIsDown_SiteReturns503_OutOfProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.OutOfProcess);

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);
        DeletePublishOutput(deploymentResult);
    }

    [ConditionalFact]
    public async Task LockedAppOfflineDroppedWhileSiteIsDown_SiteReturns503_InProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.InProcess);

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

    [ConditionalFact]
    [RequiresNewShim]
    public async Task LockedAppOfflineDroppedWhileSiteIsDown_SiteReturns503_OutOfProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.OutOfProcess);

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

    [ConditionalFact]
    public async Task AppOfflineDroppedWhileSiteFailedToStartInShim_AppOfflineServed_InProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: HostingModel.InProcess);
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "nonexistent"));

        var deploymentResult = await DeployAsync(deploymentParameters);

        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.Equal(500, (int)result.StatusCode);
        Assert.Contains("500.0", await result.Content.ReadAsStringAsync());

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);
        DeletePublishOutput(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task AppOfflineDroppedWhileSiteFailedToStartInShim_AppOfflineServed_OutOfProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: HostingModel.OutOfProcess);
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "nonexistent"));

        var deploymentResult = await DeployAsync(deploymentParameters);

        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.Equal(502, (int)result.StatusCode);
        Assert.Contains("502.5", await result.Content.ReadAsStringAsync());

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);
        DeletePublishOutput(deploymentResult);
    }

    [ConditionalFact]
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
    public async Task GracefulShutdownWorksWithMultipleRequestsInFlight_InProcess()
    {
        // The goal of this test is to have multiple requests currently in progress
        // and for app offline to be dropped. We expect that all requests are eventually drained
        // and graceful shutdown occurs.
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
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
            EventLogHelpers.ShutdownMessage(deploymentResult), Logger);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task RequestsWhileRestartingAppFromConfigChangeAreProcessed()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

        if (deploymentParameters.ServerType == ServerType.IISExpress)
        {
            // IISExpress doesn't support recycle
            return;
        }

        var deploymentResult = await DeployAsync(deploymentParameters);

        var result = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.Dispose();

        // Just "touching" web.config should be enough to restart the process
        deploymentResult.ModifyWebConfig(element => { });

        // Default shutdown delay is 1 second, we want to send requests while the shutdown is happening
        // So we send a bunch of requests and one of them hopefully will run during shutdown and be queued for processing by the new app
        for (var i = 0; i < 2000; i++)
        {
            using var res = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            await Task.Delay(1);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        await deploymentResult.AssertRecycledAsync();

        // Shutdown should be graceful here!
        EventLogHelpers.VerifyEventLogEvent(deploymentResult,
            EventLogHelpers.ShutdownMessage(deploymentResult), Logger);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task RequestsWhileRecyclingAppAreProcessed()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

        if (deploymentParameters.ServerType == ServerType.IISExpress)
        {
            // IISExpress doesn't support recycle
            return;
        }

        var deploymentResult = await DeployAsync(deploymentParameters);

        var result = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.Dispose();

        // Recycle app pool
        Helpers.Recycle(deploymentResult.AppPoolName);

        // Default shutdown delay is 1 second, we want to send requests while the shutdown is happening
        // So we send a bunch of requests and one of them hopefully will run during shutdown and be queued for processing by the new app
        for (var i = 0; i < 2000; i++)
        {
            using var res = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            await Task.Delay(1);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        await deploymentResult.AssertRecycledAsync();

        // Shutdown should be graceful here!
        EventLogHelpers.VerifyEventLogEvent(deploymentResult,
            EventLogHelpers.ShutdownMessage(deploymentResult), Logger);
    }

    [ConditionalFact]
    public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_InProcess()
    {
        var deploymentResult = await AssertStarts(HostingModel.InProcess);

        AddAppOffline(deploymentResult.ContentRoot);

        await deploymentResult.AssertRecycledAsync(() => AssertAppOffline(deploymentResult));
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task AppOfflineDroppedWhileSiteRunning_SiteShutsDown_OutOfProcess()
    {
        var deploymentResult = await AssertStarts(HostingModel.OutOfProcess);

        AddAppOffline(deploymentResult.ContentRoot);
        await AssertAppOffline(deploymentResult);
        RemoveAppOffline(deploymentResult.ContentRoot);
        await AssertRunning(deploymentResult);

        AddAppOffline(deploymentResult.ContentRoot);
        await AssertAppOffline(deploymentResult);
        DeletePublishOutput(deploymentResult);
    }

    [ConditionalFact]
    public async Task AppOfflineDropped_CanRemoveAppOfflineAfterAddingAndSiteWorks_InProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.InProcess);

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);

        RemoveAppOffline(deploymentResult.ContentRoot);

        await AssertRunning(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task AppOfflineDropped_CanRemoveAppOfflineAfterAddingAndSiteWorks_OutOfProcess()
    {
        var deploymentResult = await DeployApp(HostingModel.OutOfProcess);

        AddAppOffline(deploymentResult.ContentRoot);

        await AssertAppOffline(deploymentResult);

        RemoveAppOffline(deploymentResult.ContentRoot);

        await AssertRunning(deploymentResult);
    }

    [ConditionalFact]
    [SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
    public async Task AppOfflineAddedAndRemovedStress_InProcess()
    {
        await AppOfflineAddAndRemovedStress(HostingModel.InProcess);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task AppOfflineAddedAndRemovedStress_OutOfProcess()
    {
        await AppOfflineAddAndRemovedStress(HostingModel.OutOfProcess);
    }

    private async Task AppOfflineAddAndRemovedStress(HostingModel hostingModel)
    {
        var deploymentResult = await AssertStarts(hostingModel);

        var load = Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response =>
        {
            var statusCode = (int)response.StatusCode;
            // Test failure involves the stress load receiving a 400 Bad Request.
            // We think it is due to IIS returning the 400 itself, but need to confirm the hypothesis.
            if (statusCode == 400)
            {
                Logger.LogError($"Status code was a bad request. Content: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
            Assert.True(statusCode == 200 || statusCode == 503, "Status code was " + statusCode);
        });

        for (int i = 0; i < 5; i++)
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

    [ConditionalFact]
    public async Task ConfigurationChangeStopsInProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);

        var deploymentResult = await DeployAsync(deploymentParameters);

        await deploymentResult.AssertStarts();

        // Just "touching" web.config should be enough
        deploymentResult.ModifyWebConfig(element => { });

        await deploymentResult.AssertRecycledAsync();
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task ConfigurationChangeForcesChildProcessRestart()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

        var deploymentResult = await DeployAsync(deploymentParameters);

        var processBefore = await deploymentResult.HttpClient.GetStringAsync("/ProcessId");

        // Just "touching" web.config should be enough
        deploymentResult.ModifyWebConfig(element => { });

        // Have to retry here to allow ANCM to receive notification and react to it
        // Verify that worker process gets restarted with new process id
        await deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", async r => await r.Content.ReadAsStringAsync() != processBefore);
    }

    [ConditionalFact]
    public async Task ConfigurationChangeCanBeIgnoredInProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
        deploymentParameters.HandlerSettings["disallowRotationOnConfigChange"] = "true";

        var deploymentResult = await DeployAsync(deploymentParameters);

        var processBefore = await deploymentResult.HttpClient.GetStringAsync("/ProcessId");

        await deploymentResult.AssertStarts();

        // Just "touching" web.config should be enough
        deploymentResult.ModifyWebConfig(element => { });

        // Have to retry here to allow ANCM to receive notification and react to it
        // Verify that worker process does not get restarted with new process id
        await deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", async r => await r.Content.ReadAsStringAsync() == processBefore);
    }

    [ConditionalFact]
    public async Task AppHostConfigurationChangeIsIgnoredInProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
        deploymentParameters.HandlerSettings["disallowRotationOnConfigChange"] = "true";

        var deploymentResult = await DeployAsync(deploymentParameters);

        var processBefore = await deploymentResult.HttpClient.GetStringAsync("/ProcessId");

        await deploymentResult.AssertStarts();

        // Just "touching" applicationHost.config should be enough
        _deployer.ModifyApplicationHostConfig(element => { });

        // Have to retry here to allow ANCM to receive notification and react to it
        // Verify that worker process does not get restarted with new process id
        await deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", async r => await r.Content.ReadAsStringAsync() == processBefore);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task ConfigurationChangeCanBeIgnoredOutOfProcess()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.HandlerSettings["disallowRotationOnConfigChange"] = "true";

        var deploymentResult = await DeployAsync(deploymentParameters);

        var processBefore = await deploymentResult.HttpClient.GetStringAsync("/ProcessId");

        // Just "touching" web.config should be enough
        deploymentResult.ModifyWebConfig(element => { });

        // Have to retry here to allow ANCM to receive notification and react to it
        // Verify that worker process does not get restarted with new process id
        await deploymentResult.HttpClient.RetryRequestAsync("/ProcessId", async r => await r.Content.ReadAsStringAsync() == processBefore);
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/55937")]
    [ConditionalFact]
    public async Task OutOfProcessToInProcessHostingModelSwitchWorks()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

        var deploymentResult = await DeployAsync(deploymentParameters);

        await deploymentResult.AssertStarts();

        deploymentResult.ModifyWebConfig(element => element
            .Descendants("system.webServer")
            .Single()
            .GetOrAdd("aspNetCore")
            .SetAttributeValue("hostingModel", "inprocess"));

        // Have to retry here to allow ANCM to receive notification and react to it
        // Verify that inprocess application was created and started, checking the server
        // header to see that it is running inprocess
        await deploymentResult.HttpClient.RetryRequestAsync("/HelloWorld", r => r.Headers.Server.ToString().StartsWith("Microsoft", StringComparison.Ordinal));
    }

    [ConditionalFact]
    [SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
    public async Task ConfigurationTouchedStress_InProcess()
    {
        await ConfigurationTouchedStress(HostingModel.InProcess);
    }

    private async Task ConfigurationTouchedStress(HostingModel hostingModel)
    {
        var deploymentResult = await DeployAsync(Fixture.GetBaseDeploymentParameters(hostingModel));

        await deploymentResult.AssertStarts();
        var load = Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response =>
        {
            var statusCode = (int)response.StatusCode;
            Assert.True(statusCode == 200 || statusCode == 503, "Status code was " + statusCode);
        });

        for (var i = 0; i < 100; i++)
        {
            // ModifyWebConfig might fail if web.config is being read by IIS
            RetryHelper.RetryOperation(
                () => deploymentResult.ModifyWebConfig(element => { }),
                e => Logger.LogError($"Failed to touch web.config : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: RetryDelay.Milliseconds);
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

    [ConditionalFact]
    [RequiresNewShim]
    public async Task ClosesConnectionOnServerAbortOutOfProcess()
    {
        try
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/Abort").TimeoutAfter(TimeoutExtensions.DefaultTimeoutValue);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

#if NEWSHIM_FUNCTIONALS
            // In-proc SocketConnection isn't used and there's no abort
            // 0x80072f78 ERROR_HTTP_INVALID_SERVER_RESPONSE The server returned an invalid or unrecognized response
            Assert.Contains("0x80072f78", await response.Content.ReadAsStringAsync());
#else
            // 0x80072efe ERROR_INTERNET_CONNECTION_ABORTED The connection with the server was terminated abnormally
            Assert.Contains("0x80072efe", await response.Content.ReadAsStringAsync());
#endif
        }
        catch (HttpRequestException)
        {
            // Connection reset is expected
        }
    }

    [ConditionalFact]
    public async Task ClosesConnectionOnServerAbortInProcess()
    {
        try
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("/Abort").TimeoutAfter(TimeoutExtensions.DefaultTimeoutValue);

            Assert.True(false, "Should not reach here");
        }
        catch (HttpRequestException)
        {
            // Connection reset is expected both for outofproc and inproc
        }
    }
}
