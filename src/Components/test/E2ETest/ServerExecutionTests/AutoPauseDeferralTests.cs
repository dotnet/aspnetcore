// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Json;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class AutoPauseDeferralTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    // Must be short enough to keep tests fast but long enough to be reliably observable.
    private const int PauseDelayMs = 200;

    public AutoPauseDeferralTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"/subdir/persistent-state/auto-pause-download?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        // Only rendered when the component is interactive — reliable readiness signal.
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    // server streams bytes to JS over the circuit, pausing would cause exceptions
    public void DotNetStreamReference_DoesNotPause_WhileStreamInFlight()
        => RunDeferralTest("streamref-button", expectDeferral: true);

    [Fact]
    // browser opens download in new tab, circuit not involved, pause as normal
    public void AnchorTargetBlank_PausesNormally_WhileHttpDownloadInFlight()
        => RunDeferralTest("anchor-blank-link", expectDeferral: false);

    [Fact]
    // browser handles attachment download, circuit not involved, pause as normal
    public void AnchorDownloadAttribute_PausesNormally_WhileHttpDownloadInFlight()
        => RunDeferralTest("anchor-download-link", expectDeferral: false);

    [Fact]
    // full-page navigation that bypasses the circuit
    public void NavigateToForceLoad_DoesNotCrashCircuit()
    {
        var token = ReadToken("navigate-button");
        Browser.Exists(By.Id("navigate-button")).Click();
        WaitForStreamStarted(token);
        ReleaseGate(token);
    }

    private void RunDeferralTest(string elementId, bool expectDeferral)
    {
        var token = ReadToken(elementId);

        Browser.Exists(By.Id(elementId)).Click();
        WaitForStreamStarted(token);

        ClearBlazorLogs();
        SetVisibility("hidden");

        try
        {
            if (expectDeferral)
            {
                WaitForBlazorLog("Pause deferred:");
                Assert.False(IsReconnectModalShown(),
                    "Deferral was logged but the reconnect modal is showing — pause should be held until streams drain.");
            }
            else
            {
                WaitForPausedUI();
            }
        }
        finally
        {
            ReleaseGate(token);
        }

        if (expectDeferral)
        {
            // Modal only appears after the drain releases the deferred pause.
            WaitForPausedUI();
        }

        AssertNoConsoleErrors();

        SetVisibility("visible");
        WaitForResumedUI();
    }

    private string ReadToken(string elementId)
    {
        var token = Browser.Exists(By.Id(elementId)).GetDomAttribute("data-token");
        Assert.False(string.IsNullOrEmpty(token), $"Element {elementId} is missing a data-token attribute.");
        return token!;
    }

    private bool IsReconnectModalShown()
    {
        var modals = Browser.FindElements(By.Id("components-reconnect-modal"));
        if (modals.Count == 0)
        {
            return false;
        }
        var display = modals[0].GetCssValue("display");
        return display == "block";
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

    private static readonly TimeSpan LogWaitTimeout = TimeSpan.FromSeconds(10);

    private void ClearBlazorLogs()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.__blazorLogs && (window.__blazorLogs.length = 0);");
    }

    private void WaitForBlazorLog(string substring)
    {
        var deadline = DateTime.UtcNow + LogWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var found = (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
                "var s = arguments[0]; return !!(window.__blazorLogs && window.__blazorLogs.some(function (e) { return e.msg && e.msg.indexOf(s) >= 0; }));",
                substring);
            if (found)
            {
                return;
            }
            Thread.Sleep(25);
        }
        throw new TimeoutException($"Timed out after {LogWaitTimeout.TotalSeconds}s waiting for Blazor log line containing: \"{substring}\".");
    }

    private void WaitForPausedUI()
    {
        Browser.Equal("block", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    private void WaitForResumedUI()
    {
        // After visibility returns the modal is hidden again (display != "block").
        Browser.NotEqual("block", () =>
        {
            var modals = Browser.FindElements(By.Id("components-reconnect-modal"));
            return modals.Count == 0 ? "none" : modals[0].GetCssValue("display");
        });
    }

    private void AssertNoConsoleErrors()
    {
        var severeEntries = Browser.Manage().Logs.GetLog(LogType.Browser)
            .Where(e => e.Level == OpenQA.Selenium.LogLevel.Severe)
            // Only flag the SignalR-rejection symptom we care about; other
            // unrelated Severe entries are out of scope for this test.
            .Where(e => e.Message.Contains("Cannot send data", StringComparison.OrdinalIgnoreCase)
                     || e.Message.Contains("HubException", StringComparison.OrdinalIgnoreCase)
                     || e.Message.Contains("Invocation canceled", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(severeEntries.Count == 0,
            "Browser console reported SignalR-related errors after the gated download:\n  " +
            string.Join("\n  ", severeEntries.Select(e => e.Message)));
    }

    private void WaitForStreamStarted(string token)
        => PollServerFlag(token, "started", "stream did not start");

    private void PollServerFlag(string token, string flagName, string failureMessage)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        using var http = NewHttpClient();
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = http.GetAsync($"/subdir/autopause-test/{flagName}/{token}").GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var payload = response.Content.ReadFromJsonAsync<Dictionary<string, bool>>().GetAwaiter().GetResult();
                    if (payload != null && payload.TryGetValue(flagName, out var value) && value)
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Server may briefly be unreachable; retry.
            }
            Thread.Sleep(50);
        }
        throw new TimeoutException($"Timed out after 30s waiting for {flagName} flag on token {token}: {failureMessage}.");
    }

    private void ReleaseGate(string token)
    {
        using var http = NewHttpClient();
        using var response = http.PostAsync($"/subdir/autopause-test/release/{token}", content: null).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }

    private HttpClient NewHttpClient()
    {
        var serverRoot = new Uri(Browser.Url).GetLeftPart(UriPartial.Authority);
        return new HttpClient { BaseAddress = new Uri(serverRoot) };
    }
}
