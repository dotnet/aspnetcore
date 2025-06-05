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

public class ServerResumeTestsTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public ServerResumeTestsTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
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
    public void CanResumeCircuitAfterDisconnection()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        var previousText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.replaceReconnectCallback()");
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        javascript.ExecuteScript("triggerReconnect()");

        // Then it should disappear
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        var newText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        Assert.NotEqual(previousText, newText);

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }
}
