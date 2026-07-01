// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class AutoPauseTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public AutoPauseTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/subdir/persistent-state/server-pause?auto-pause=true&auto-pause-delay-ms=200");
        Browser.Exists(By.Id("render-mode-interactive"));
        WaitForBlazorPause();
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            window.myApp.deferPause(async (signal) => {
                window.autoPauseEvents.push({ phase: 'testLog:handlerInvoked', aborted: signal.aborted });
            });
        ");
    }

    [Fact]
    public void HiddenTab_PausesCircuit_AndResumesOnVisible()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        // automation driver tab switches do not trigger visibilitychange, so set it explicitly
        SetVisibility("hidden");

        WaitForPausedUI();

        // Pause handler fired before pause.
        Assert.NotEmpty(GetAutoPauseEvents());

        SetVisibility("visible");

        WaitForResumedUI();

        // State preserved: counter still shows 1, and we can keep interacting.
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void AutoPause_StillWorks_AfterAPauseResumeCycle()
    {
        // First pause/resume cycle.
        SetVisibility("hidden");
        WaitForPausedUI();
        SetVisibility("visible");
        WaitForResumedUI();

        // The AutoPauseManager must survive the resume so that a subsequent hide pauses again. 
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        SetVisibility("hidden");
        WaitForPausedUI();

        SetVisibility("visible");
        WaitForResumedUI();

        // State preserved across both cycles.
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void HiddenTab_BecomesVisibleBeforeDelay_DoesNotPause()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        // Hide and re-show in the same JS turn. The AutoPauseManager schedules its
        // timer on visibilitychange and cancels it on the next one, so this proves
        // the pause is never queued — no time-based waiting is required.
        SetVisibility("hidden");
        SetVisibility("visible");

        // If the circuit had auto-paused, this click would not reach the server
        // (the reconnect modal would intercept) and the count would not advance.
        // Successful increment plus no recorded pause handler invocation is
        // the deterministic proof that no pause occurred.
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Assert.Empty(GetAutoPauseEvents());
    }

    private void SetVisibility(string state)
    {
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript($@"
            Object.defineProperty(document, 'visibilityState', {{ configurable: true, get: () => '{state}' }});
            Object.defineProperty(document, 'hidden', {{ configurable: true, get: () => {(state == "hidden" ? "true" : "false")} }});
            document.dispatchEvent(new Event('visibilitychange'));
        ");
    }

    private IReadOnlyList<string> GetAutoPauseEvents()
    {
        var js = (IJavaScriptExecutor)Browser;
        var raw = js.ExecuteScript("return JSON.stringify(window.autoPauseEvents || []);") as string;
        if (string.IsNullOrEmpty(raw) || raw == "[]")
        {
            return Array.Empty<string>();
        }
        // Cheap split — each entry is a JSON object. Good enough for assertions.
        return raw.Trim('[', ']').Split("},{").Select(s => s.Trim('{', '}')).ToList();
    }

    private void WaitForBlazorPause()
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return !!(window.Blazor && window.myApp && typeof window.myApp.deferPause === 'function')"));
    }

    private void WaitForPausedUI()
    {
        Browser.Equal("block", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    private void WaitForResumedUI()
    {
        Browser.Equal("none", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }
}
