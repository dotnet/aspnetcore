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

public class ServerResumeTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public ServerResumeTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(TestUrl);
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    public string TestUrl { get; set; } = "/subdir/persistent-state/disconnection";

    public bool UseShadowRoot { get; set; } = true;

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

    [Fact]
    public void CanResumeCircuitFromJavaScript()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        var javascript = (IJavaScriptExecutor)Browser;
        TriggerClientPauseAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        TriggerClientPauseAndInteract(javascript);

        // Ensure that reconnection events are repeatable
        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void CanResumeUngracefullyPauseGracefullyPauseUngracefullyAgain()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.replaceReconnectCallback()");
        TriggerReconnectAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        TriggerClientPauseAndInteract(javascript);

        // Ensure that reconnection events are repeatable
        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        javascript.ExecuteScript("resetReconnect()");

        TriggerReconnectAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("4", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void CanPauseGracefullyUngracefulPauseGracefullyPauseAgain()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.replaceReconnectCallback()");
        TriggerClientPauseAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        TriggerReconnectAndInteract(javascript);

        // Ensure that reconnection events are repeatable
        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        TriggerClientPauseAndInteract(javascript);

        // Can dispatch events after reconnect
        Browser.Equal("4", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
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

    private void TriggerClientPauseAndInteract(IJavaScriptExecutor javascript)
    {
        var previousText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        javascript.ExecuteScript("Blazor.pause()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        // Retry button should be hidden
        Browser.Equal(
            (false, true),
            () => Browser.Exists(
                () =>
                {
                    var buttons = UseShadowRoot ?
                        Browser.Exists(By.Id("components-reconnect-modal"))
                            .GetShadowRoot()
                            .FindElements(By.CssSelector(".components-reconnect-dialog button")) :
                        Browser.Exists(By.Id("components-reconnect-modal"))
                            .FindElements(By.CssSelector(".components-reconnect-container button"));

                    Assert.Equal(2, buttons.Count);
                    return (buttons[0].Displayed, buttons[1].Displayed);
                },
                TimeSpan.FromSeconds(1)));

        Browser.Exists(
                () =>
                {
                    var buttons = UseShadowRoot ?
                        Browser.Exists(By.Id("components-reconnect-modal"))
                            .GetShadowRoot()
                            .FindElements(By.CssSelector(".components-reconnect-dialog button")) :
                        Browser.Exists(By.Id("components-reconnect-modal"))
                            .FindElements(By.CssSelector(".components-reconnect-container button"));
                    return buttons[1];
                },
                TimeSpan.FromSeconds(1)).Click();

        // Then it should disappear
        Browser.Equal("none", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        var newText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        Assert.NotEqual(previousText, newText);

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
    }
}

public class CustomUIServerResumeTests : ServerResumeTests
{
    public CustomUIServerResumeTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        TestUrl = "/subdir/persistent-state/disconnection?custom-reconnect-ui=true";
        UseShadowRoot = false; // Custom UI does not use shadow DOM
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        Browser.Exists(By.CssSelector("#components-reconnect-modal[data-nosnippet]"));
    }
}

public class HybridCacheServerResumeTests : ServerResumeTests
{
    public HybridCacheServerResumeTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--UseHybridCache", "true");
    }
}
