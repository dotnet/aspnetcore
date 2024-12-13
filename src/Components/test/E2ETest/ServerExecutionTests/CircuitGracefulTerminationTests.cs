// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class CircuitGracefulTerminationTests : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>, IDisposable
{
    public CircuitGracefulTerminationTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public TaskCompletionSource GracefulDisconnectCompletionSource { get; private set; }
    public TestSink Sink { get; private set; }
    public List<(Extensions.Logging.LogLevel level, string eventIdName)> Messages { get; private set; }

    public override async Task InitializeAsync()
    {
        // These tests manipulate the browser in  ways that make it impossible to use the same browser
        // instance across tests (One of the tests closes the browser). For that reason we simply create
        // a new browser instance for every test in this class sos that there are no issues when running
        // them together.
        await base.InitializeAsync(Guid.NewGuid().ToString());
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<GracefulTermination>();
        Browser.Equal("Graceful Termination", () => Browser.Exists(By.TagName("h1")).Text);

        GracefulDisconnectCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
        Messages = new List<(Extensions.Logging.LogLevel level, string eventIdName)>();
        Sink.MessageLogged += Log;
    }

    [Fact]
    public async Task ReloadingThePage_GracefullyDisconnects_TheCurrentCircuit()
    {
        // Arrange & Act
        Browser.Navigate().Refresh();
        await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    [Fact]
    public async Task ClosingTheBrowserWindow_GracefullyDisconnects_TheCurrentCircuit()
    {
        // Arrange & Act
        Browser.Close();
        await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.True(GracefulDisconnectCompletionSource.Task.IsCompletedSuccessfully);
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    [Fact]
    public async Task ClosingTheBrowserWindow_GracefullyDisconnects_WhenNavigatingAwayFromThePage()
    {
        // Arrange & Act
        Browser.Navigate().GoToUrl("about:blank");
        var task = await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.Equal(GracefulDisconnectCompletionSource.Task, task);
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.Contains((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    [Fact]
    public async Task NavigatingToProtocolLink_DoesNotGracefullyDisconnect_TheCurrentCircuit()
    {
        // Arrange & Act
        var element = Browser.Exists(By.Id("mailto-link"));
        element.Click();
        await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    [Fact]
    public async Task DownloadAction_DoesNotGracefullyDisconnect_TheCurrentCircuit()
    {
        // Arrange & Act
        var element = Browser.Exists(By.Id("download-link"));
        element.Click();
        await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    [Fact]
    public async Task DownloadHref_DoesNotGracefullyDisconnect_TheCurrentCircuit()
    {
        // Arrange & Act
        var element = Browser.Exists(By.Id("download-href"));
        element.Click();
        await Task.WhenAny(Task.Delay(10000), GracefulDisconnectCompletionSource.Task);

        // Assert
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully"), Messages.ToArray());
        Assert.DoesNotContain((Extensions.Logging.LogLevel.Debug, "CircuitDisconnectedPermanently"), Messages.ToArray());
    }

    private void Log(WriteContext wc)
    {
        if ((Extensions.Logging.LogLevel.Debug, "CircuitTerminatedGracefully") == (wc.LogLevel, wc.EventId.Name))
        {
            GracefulDisconnectCompletionSource.TrySetResult();
        }
        Messages.Add((wc.LogLevel, wc.EventId.Name));
    }

    public void Dispose()
    {
        if (Sink != null)
        {
            Sink.MessageLogged -= Log;
        }
    }
}
