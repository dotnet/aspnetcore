// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using BasicTestApp.Reconnection;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerResumeTestsTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public ServerResumeTestsTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/subdir/persistent-state/disconnection");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public async Task CanResumeCircuitAfterDisconnection()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        var previousText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        await Task.Delay(5000);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
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
