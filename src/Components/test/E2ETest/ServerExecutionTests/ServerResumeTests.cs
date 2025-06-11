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
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.replaceReconnectCallback()");

        TriggerReconnectAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        javascript.ExecuteScript("resetReconnect()");

        TriggerReconnectAndInteract(javascript);

        // Ensure that reconnection events are repeatable
        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    private void TriggerReconnectAndInteract(IJavaScriptExecutor javascript)
    {
        var previousText = Browser.Exists(By.Id("persistent-counter-render")).Text;

        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        javascript.ExecuteScript("triggerReconnect()");

        // Then it should disappear
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        var newText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        Assert.NotEqual(previousText, newText);

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
    }
}
