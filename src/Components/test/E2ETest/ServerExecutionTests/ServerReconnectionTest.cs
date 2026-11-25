// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using BasicTestApp;
using BasicTestApp.Reconnection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<DelayedWebSocketServerStartup>>
{
    public ServerReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<DelayedWebSocketServerStartup> serverFixture,
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
    public void UnacknowledgedInteractionStartsReconnectionBeforeServerTimeout()
    {
        var startup = _serverFixture.Host.Services.GetRequiredService<DelayedWebSocketServerStartup>();
        var reconnectModal = By.Id("components-reconnect-modal");

        Browser.MountTestComponent<AsyncEventHandlerComponent>();
        Browser.Exists(By.Id("tick")).Click();
        Browser.Equal("Started", () => Browser.Exists(By.Id("state")).Text);
        var observation = Stopwatch.StartNew();
        Browser.True(() => observation.Elapsed >= TimeSpan.FromSeconds(3));
        Assert.DoesNotContain(Browser.FindElements(reconnectModal), element => element.GetCssValue("display") is not "none");
        Browser.Exists(By.Id("tock")).Click();
        Browser.Equal("Stopped", () => Browser.Exists(By.Id("state")).Text);

        Browser.MountTestComponent<ReconnectionComponent>();
        Browser.Exists(By.Id("increment")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("count")).Text);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var count = Browser.Exists(By.Id("count")).Text;
            try
            {
                startup.PauseIncomingMessages();
                Browser.Exists(By.Id("increment")).Click();

                Browser.True(
                    () => Browser.FindElements(reconnectModal).Any(element =>
                        element.GetShadowRoot().FindElement(By.CssSelector(".components-reconnect-overlay")).Displayed),
                    TimeSpan.FromSeconds(3),
                    "An unacknowledged interaction should show the reconnect UI before the server timeout.");
                Assert.Equal(count, Browser.Exists(By.Id("count")).Text);
            }
            finally
            {
                startup.ResumeIncomingMessages();
            }

            Browser.Equal("none", () => Browser.Exists(reconnectModal).GetCssValue("display"));
            var reconnectedCount = int.Parse(Browser.Exists(By.Id("count")).Text, CultureInfo.InvariantCulture);
            Browser.Exists(By.Id("increment")).Click();
            Browser.Equal((reconnectedCount + 1).ToString(CultureInfo.InvariantCulture), () => Browser.Exists(By.Id("count")).Text);
        }
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
    }
}
