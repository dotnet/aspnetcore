// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp.Reconnection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public ServerReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ReconnectionComponent>();
        Browser.Exists(By.Id("count"));
    }

    [Fact]
    public void ReconnectUI()
    {
        Browser.Exists(By.Id("increment")).Click();

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // We should see the 'reconnecting' UI appear
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        // Then it should disappear
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        Browser.Exists(By.Id("increment")).Click();

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("count")).Text);
    }

    [Fact]
    public void FailedReconnectDoesNotShowErrorUI_WithNegotiation()
    {
        FailedReconnectDoesNotShowErrorUI(skipNegotiation: false);
    }

    [Fact]
    public void FailedReconnectDoesNotShowErrorUI_WithoutNegotiation()
    {
        Navigate($"{ServerPathBase}?skipNegotiation=true");
        Browser.MountTestComponent<ReconnectionComponent>();
        Browser.Exists(By.Id("count"));

        FailedReconnectDoesNotShowErrorUI(skipNegotiation: true);
    }

    private void FailedReconnectDoesNotShowErrorUI(bool skipNegotiation)
    {
        Browser.Exists(By.Id("increment")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("count")).Text);

        var chromeDriver = (ChromeDriver)Browser;
        var javascript = (IJavaScriptExecutor)Browser;
        var timeOrigin = javascript.ExecuteScript("return performance.timeOrigin");
        var logs = new List<LogEntry>();
        string errorDisplayDuringRetry;

        chromeDriver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>());
        try
        {
            SetOffline(chromeDriver, offline: true);
            javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

            Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
            Browser.True(() =>
            {
                logs.AddRange(Browser.Manage().Logs.GetLog(LogType.Browser));
                return logs.Any(entry => entry.Message.Contains(skipNegotiation
                    ? "Error: Error: WebSocket failed to connect"
                    : "Error: Error: Failed to complete negotiation"));
            });
            errorDisplayDuringRetry = Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display");
        }
        finally
        {
            SetOffline(chromeDriver, offline: false);
            chromeDriver.ExecuteCdpCommand("Network.disable", new Dictionary<string, object>());
        }

        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
        Browser.Exists(By.Id("increment")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("count")).Text);
        Assert.Equal(timeOrigin, javascript.ExecuteScript("return performance.timeOrigin"));

        Browser.Equal("none", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
        Assert.Equal("none", errorDisplayDuringRetry);
        logs.AddRange(Browser.Manage().Logs.GetLog(LogType.Browser));
        Assert.DoesNotContain(logs, entry => entry.Message.Contains("Cannot send data if the connection is not in the 'Connected' State."));
    }

    private static void SetOffline(ChromeDriver chromeDriver, bool offline)
    {
        chromeDriver.ExecuteCdpCommand("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = offline,
            ["latency"] = 0,
            ["downloadThroughput"] = -1,
            ["uploadThroughput"] = -1,
        });
    }

    [Fact]
    public void RendersContinueAfterReconnect()
    {
        var selector = By.Id("ticker");
        var element = Browser.Exists(selector);

        var initialValue = element.Text;

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // We should see the 'reconnecting' UI appear
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        // Then it should disappear
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        // We should receive a render that occurred while disconnected
        var currentValue = Browser.Exists(selector).Text;
        Assert.NotEqual(initialValue, currentValue);

        // Verify it continues to tick
        Thread.Sleep(5);
        Browser.False(() => Browser.Exists(selector).Text == currentValue);
    }

    [Fact]
    public void ErrorsStopTheRenderingProcess()
    {
        Browser.Exists(By.Id("cause-error")).Click();
        Browser.True(() => Browser.Manage().Logs.GetLog(LogType.Browser)
            .Any(l => l.Level == LogLevel.Info && l.Message.Contains("Connection disconnected.")));
        Browser.Equal("block", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
    }
}
