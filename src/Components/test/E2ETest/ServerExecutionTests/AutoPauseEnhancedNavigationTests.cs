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

public class AutoPauseEnhancedNavigationTests : AutoPauseTestBase<App>
{
    public AutoPauseEnhancedNavigationTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutoPause_StartsWhenInteractiveServerStartsAfterEnhancedNavigation(bool streaming)
    {
        Navigate("/subdir/persistent-state/auto-pause-enhanced-navigation-landing");
        var htmlElement = Browser.Exists(By.TagName("html"));

        var linkId = streaming ? "navigate-to-streaming-auto-pause" : "navigate-to-auto-pause";
        Browser.Exists(By.Id(linkId)).Click();
        Browser.Exists(By.Id("increment-persistent-counter-count"));
        Browser.False(() => htmlElement.IsStale());

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Exists(By.Id("increment-non-persisted-counter")).Click();
        Browser.Equal("6", () => Browser.Exists(By.Id("non-persisted-counter")).Text);

        SetVisibility("hidden");
        WaitForPausedUI();

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("non-persisted-counter")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void AutoPause_DoesNotResumeDisposedCircuitAfterEnhancedNavigation()
    {
        Navigate("/subdir/persistent-state/server-pause?auto-pause=true&auto-pause-delay-ms=200&circuit-inactivity-timeout-ms=5000");
        var htmlElement = Browser.Exists(By.TagName("html"));
        Browser.NotEqual("", () => Browser.Exists(By.Id("circuit-id")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        // Chrome drains its browser log when read, isolating errors from this navigation.
        Browser.GetBrowserLogs(LogLevel.Severe);
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("""
            window.resumeAttempts = 0;
            window.visibilityChanges = [];
            const resumeCircuit = Blazor.resumeCircuit;
            Blazor.resumeCircuit = (...args) => {
                window.resumeAttempts++;
                return resumeCircuit(...args);
            };
            document.addEventListener('visibilitychange', () => window.visibilityChanges.push(document.visibilityState));
            """);

        Browser.Exists(By.Id("navigate-to-static-auto-pause")).Click();
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);
        Browser.False(() => htmlElement.IsStale());

        WithHiddenTab(hiddenJs =>
        {
            Browser.Equal("block", () => hiddenJs.ExecuteScript(
                "return window.testWindow.getComputedStyle(window.testWindow.document.getElementById('components-reconnect-modal')).display;"));
            Browser.True(() => (bool)hiddenJs.ExecuteScript(
                "return window.testWindow.performance.getEntriesByType('resource').some(entry => entry.name.includes('/_blazor/disconnect'));"),
                TimeSpan.FromSeconds(30));
        });

        Browser.True(() => (bool)js.ExecuteScript("return window.visibilityChanges.at(-1) === 'visible';"));
        Browser.Equal("static", () => Browser.Exists(By.Id("execution-mode")).Text);
        WaitForResumedUI();
        Assert.Equal(0L, js.ExecuteScript("return window.resumeAttempts;"));
        Assert.DoesNotContain(Browser.GetBrowserLogs(LogLevel.Severe),
            entry => entry.Message.Contains("The circuit associated with this dispatcher is no longer available", StringComparison.Ordinal));

        Browser.Exists(By.Id("navigate-to-auto-pause")).Click();
        Browser.NotEqual("", () => Browser.Exists(By.Id("circuit-id")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.False(() => htmlElement.IsStale());

        WithHiddenTab(hiddenJs => Browser.Equal("block", () => hiddenJs.ExecuteScript(
            "return window.testWindow.getComputedStyle(window.testWindow.document.getElementById('components-reconnect-modal')).display;")));
        WaitForResumedUI();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void AutoPause_PausesReplacementAfterAbortedPauseDeferralCompletes()
    {
        Navigate("/subdir/persistent-state/server-pause?auto-pause=true&auto-pause-delay-ms=200&suppress-autostart");
        var htmlElement = Browser.Exists(By.TagName("html"));
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("""
            const gate = Promise.withResolvers();
            window.pauseDeferral = { entered: false, aborted: false, settled: false, disposed: false, release: gate.resolve };
            Blazor._internal.circuitEvents.addEventListener('disposed', () => pauseDeferral.disposed = true, { once: true });
            Blazor.start({
                ssr: { circuitInactivityTimeoutMs: 200 },
                circuit: {
                    circuitHandlers: [{
                        onCircuitPausing: async signal => {
                            if (!pauseDeferral.entered) {
                                pauseDeferral.entered = true;
                                signal.addEventListener('abort', () => pauseDeferral.aborted = true, { once: true });
                                await gate.promise;
                                pauseDeferral.settled = true;
                            }
                        }
                    }]
                }
            });
            """);
        Browser.NotEqual("", () => Browser.Exists(By.Id("circuit-id")).Text);
        var originalCircuitId = Browser.Exists(By.Id("circuit-id")).Text;
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.GetBrowserLogs(LogLevel.Severe);

        WithHiddenTab(hiddenJs =>
        {
            Browser.True(() => (bool)hiddenJs.ExecuteScript("return window.testWindow.pauseDeferral.entered;"));
            hiddenJs.ExecuteScript("window.testWindow.document.getElementById('navigate-to-static-auto-pause').click();");
            Browser.True(() => (bool)hiddenJs.ExecuteScript("return window.testWindow.pauseDeferral.disposed;"));
            hiddenJs.ExecuteScript("window.testWindow.document.getElementById('navigate-to-auto-pause').click();");
            Browser.True(() => (bool)hiddenJs.ExecuteScript("""
                const id = window.testWindow.document.getElementById('circuit-id')?.textContent;
                return !!id && id !== arguments[0];
                """, originalCircuitId));
        });

        Browser.False(() => htmlElement.IsStale());
        Assert.True((bool)js.ExecuteScript("return pauseDeferral.aborted && !pauseDeferral.settled;"));
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        WithHiddenTab(hiddenJs =>
        {
            hiddenJs.ExecuteScript("window.testWindow.pauseDeferral.release();");
            Browser.True(() => (bool)hiddenJs.ExecuteScript("return window.testWindow.pauseDeferral.settled;"));
            Browser.True(() => (bool)hiddenJs.ExecuteScript("""
                const modal = window.testWindow.document.getElementById('components-reconnect-modal');
                return !!modal && window.testWindow.getComputedStyle(modal).display === 'block';
                """));
        });

        WaitForResumedUI();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
        Assert.Empty(Browser.GetBrowserLogs(LogLevel.Severe));
    }

    private void WithHiddenTab(Action<IJavaScriptExecutor> action)
    {
        var originalWindow = Browser.CurrentWindowHandle;
        var existingWindows = Browser.WindowHandles;
        // WebDriver cannot evaluate an unselected tab, so let the cover tab observe its opener.
        ((IJavaScriptExecutor)Browser).ExecuteScript("""
            const button = document.createElement('button');
            button.id = 'hide-auto-pause-tab';
            button.textContent = 'Hide tab';
            button.addEventListener('click', () => {
                button.remove();
                window.open('about:blank', '_blank').testWindow = window;
            });
            document.body.appendChild(button);
            """);
        Browser.Exists(By.Id("hide-auto-pause-tab")).Click();
        var coverWindow = Browser.WindowHandles.Except(existingWindows).Single();
        Browser.SwitchTo().Window(coverWindow);
        try
        {
            var js = (IJavaScriptExecutor)Browser;
            Browser.True(() => (string)js.ExecuteScript("return window.testWindow.document.visibilityState;") == "hidden",
                TimeSpan.FromSeconds(30));
            action(js);
        }
        finally
        {
            Browser.Close();
            Browser.SwitchTo().Window(originalWindow);
        }
    }

}
