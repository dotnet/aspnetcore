// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Communication;
using OpenQA.Selenium.DevTools;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;

public class ServerReconnectionWithoutStateTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public ServerReconnectionWithoutStateTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
        serverFixture.AdditionalArguments.AddRange("--DisableCircuitPersistence", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(TestUrl);
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    public string TestUrl { get; set; } = "/subdir/persistent-state/disconnection";

    public bool UseCustomReconnectionUI { get; set; }

    [Fact]
    public void ReloadsPage_AfterDisconnection_WithoutServerState()
    {
        // Check interactivity
        Browser.Equal("5", () => Browser.Exists(By.Id("non-persisted-counter")).Text);
        Browser.Exists(By.Id("increment-non-persisted-counter")).Click();
        Browser.Equal("6", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        // Store a reference to an element to detect page reload
        // When the page reloads, this element reference will become stale
        var initialElement = Browser.Exists(By.Id("non-persisted-counter"));
        var initialConnectedLogCount = GetConnectedLogCount();

        // Force close the connection
        // The client should get rejected on both reconnection and circuit resume because the server has no state
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // Check for page reload using multiple conditions:
        // 1. Previously captured element is stale
        Browser.True(initialElement.IsStale);
        // 2. Counter state is reset
        Browser.Equal("5", () => Browser.Exists(By.Id("non-persisted-counter")).Text);
        // 3. WebSocket connection has been re-established
        Browser.True(() => GetConnectedLogCount() == initialConnectedLogCount + 1);

        int GetConnectedLogCount() => Browser.Manage().Logs.GetLog(LogType.Browser)
            .Where(l => l.Level == LogLevel.Info && l.Message.Contains("Information: WebSocket connected")).Count();
    }

    [Fact]
    public void CanResume_AfterClientPause_WithoutServerState()
    {
        // Initial state: NonPersistedCounter should be 5
        Browser.Equal("5", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        // Increment both counters
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Exists(By.Id("increment-non-persisted-counter")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Equal("6", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        PauseAndResumeClient();
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        // After first reconnection:
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        // Increment non-persisted counter again
        Browser.Exists(By.Id("increment-non-persisted-counter")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        PauseAndResumeClient();
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();

        // After second reconnection:
        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("non-persisted-counter")).Text);
    }

    private void PauseAndResumeClient()
    {
        var previousText = Browser.Exists(By.Id("persistent-counter-render")).Text;
        (Browser as IJavaScriptExecutor).ExecuteScript("Blazor.pauseCircuit()");
        Browser.Equal("block", () => Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));

        // Retry button should be hidden, Resume button should be visible
        var (retry, resume) = Browser.Exists(
            () =>
            {
                // Custom UI does not use shadow DOM
                var modal = Browser.Exists(By.Id("components-reconnect-modal"));
                var buttons = UseCustomReconnectionUI
                    ? modal.FindElements(By.CssSelector(".components-reconnect-container button"))
                    : modal.GetShadowRoot().FindElements(By.CssSelector(".components-reconnect-dialog button"));
                return (buttons[0], buttons[1]);
            },
            TimeSpan.FromSeconds(1));

        Browser.False(() => retry.Displayed);
        Browser.True(() => resume.Displayed);

        resume.Click();

        Browser.True(() => Browser.Exists(By.Id("persistent-counter-render")).Text != previousText);
    }
}

public class ServerReconnectionWithoutStateCustomUITest : ServerReconnectionWithoutStateTest
{
    public ServerReconnectionWithoutStateCustomUITest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        TestUrl = "/subdir/persistent-state/disconnection?custom-reconnect-ui=true";
        UseCustomReconnectionUI = true;
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        Browser.Exists(By.CssSelector("#components-reconnect-modal[data-nosnippet]"));
    }
}
