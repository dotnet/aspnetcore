// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TestServer;
using Xunit.Abstractions;
using BrowserLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class ServerBrowserConfigurationTest(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{
    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CircuitInactivityTimeoutAppliesAfterRemovingLastServerComponent()
    {
        const int serverTimeout = 3000;
        using var fixture = CreateFixture($"--BrowserConfigurationCircuitInactivityTimeoutMs={serverTimeout}");
        NavigateToStartPage(fixture);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("""
            const Socket = window.WebSocket;
            window.WebSocket = class extends Socket {
                constructor(...args) {
                    super(...args);
                    this.addEventListener('close', () => window.socketClosedAt = performance.now());
                }
            };
            """);
        StartCounter();

        javascript.ExecuteScript("window.componentRemovedAt = performance.now()");
        Browser.Click(By.Id("remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("increment-0"));
        Browser.True(() => (bool)javascript.ExecuteScript("return window.socketClosedAt !== undefined"));

        var elapsed = Convert.ToDouble(javascript.ExecuteScript("return window.socketClosedAt - window.componentRemovedAt"), CultureInfo.InvariantCulture);
        Assert.InRange(elapsed, serverTimeout - 500, serverTimeout + 1500);
    }

    [Fact]
    public void CircuitLogLevelControlsEventMessages()
    {
        using var fixture = CreateFixture($"--BrowserConfigurationLogLevel={BrowserLogLevel.Trace}");
        NavigateToStartPage(fixture);
        StartCounter();
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);

        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));
    }

    [Fact]
    public void CircuitLogLevelFromInitializerIsNotOverwrittenByNavigation()
    {
        using var fixture = CreateFixture($"--BrowserConfigurationLogLevel={BrowserLogLevel.Error}");
        NavigateToStartPage(fixture, "&initializer-circuit-log-trace");

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor.start({})");
        Browser.True(() => (bool)javascript.ExecuteScript("return window.circuitLogInitializerRan === true"));
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));
    }

    [Fact]
    public void PreserveDomDisabledReloadsFirstNavigationAndBack()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture);

        var initialDocument = Browser.FindElement(By.TagName("html"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Exists(By.Id("remove-counter-link-0"));
        Browser.True(initialDocument.IsStale, TimeSpan.FromSeconds(10));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);

        var counterDocument = Browser.FindElement(By.TagName("html"));
        Browser.Navigate().Back();
        Browser.Equal("Not streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.True(counterDocument.IsStale);
    }

    [Fact]
    public async Task BrowserOptionsAreEmittedOnceForStaticPage()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture);

        using var client = new HttpClient { BaseAddress = fixture.RootUri };
        var html = await client.GetStringAsync("subdir/streaming-interactivity");
        Assert.DoesNotContain("<!--Blazor:", html, StringComparison.Ordinal);
        var parts = html.Split("<!--Blazor-Configuration:", StringSplitOptions.None);
        Assert.Equal(2, parts.Length);
        var payload = parts[1].Split("-->", StringSplitOptions.None)[0];
        using var configuration = JsonDocument.Parse(Convert.FromBase64String(payload));
        Assert.True(configuration.RootElement.GetProperty("ssr").GetProperty("disableDomPreservation").GetBoolean());
    }

    [Fact]
    public void PreserveDomDisabledOnDestinationReloadsFirstNavigationAndPreservesHistory()
    {
        using var fixture = CreateFixture("--BrowserConfigurationDisableOnCounter=true");
        NavigateToStartPage(fixture);

        var initialDocument = Browser.FindElement(By.TagName("html"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("disable-keys-link"));
        Browser.Exists(By.Id("enable-keys-link"));
        Assert.False(initialDocument.IsStale());

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Exists(By.Id("remove-counter-link-0"));
        Browser.True(initialDocument.IsStale, TimeSpan.FromSeconds(10));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);

        Browser.Navigate().Back();
        Browser.Exists(By.Id("enable-keys-link"));
        Browser.DoesNotExist(By.Id("is-interactive-0"));
    }

    [Fact]
    public void JavaScriptPreserveDomOptionOverridesServerConfiguration()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture);

        var initialDocument = Browser.FindElement(By.TagName("html"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({ ssr: { disableDomPreservation: false } })");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Assert.False(initialDocument.IsStale());
    }

    [Fact]
    public void InitializerPreserveDomOptionOverridesServerConfiguration()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture, "&initializer-preserve-dom");

        var initialDocument = Browser.FindElement(By.TagName("html"));
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor.start({})");
        Browser.True(() => (bool)javascript.ExecuteScript("return window.preserveDomInitializerRan === true"));
        javascript.ExecuteScript("Blazor.navigateTo(document.getElementById('add-server-counter-prerendered-link').href)");
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Assert.False(initialDocument.IsStale());
    }

    [Fact]
    public async Task PreserveDomDisabledOnFormPostDoesNotReplayTheSubmission()
    {
        using var fixture = CreateFixture("--BrowserConfigurationDisableOnFormPost=true");
        var operationId = Guid.NewGuid().ToString("N");
        Browser.Navigate(fixture.RootUri, $"{ServerPathBase}/forms/streaming-rendering/{operationId}?suppress-autostart");
        Browser.Exists(By.Id("send"));
        Browser.Exists(By.Id("call-blazor-start"));

        var initialBody = Browser.FindElement(By.TagName("body"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-form")).Text);
        using var client = new HttpClient { BaseAddress = fixture.RootUri };
        try
        {
            Browser.Click(By.Id("send"));
            Browser.Exists(By.Id("progress"));
            Browser.True(initialBody.IsStale, TimeSpan.FromSeconds(10));
        }
        finally
        {
            var response = await client.PostAsync($"subdir/forms/streaming-rendering/complete/{operationId}", content: null);
            response.EnsureSuccessStatusCode();
        }

        Browser.Exists(By.Id("pass"));
        Browser.Equal("1", () => Browser.Exists(By.Id("browser-options-post-count")).Text);
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-form")).Text);
        Browser.Click(By.Id("increment-form"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-form")).Text);
    }

    [Fact]
    public void PreserveDomDisabledReplacesStreamingContentAndActivatesFirstComponent()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        Browser.Navigate(fixture.RootUri, $"{ServerPathBase}/streaming-interactivity?ShouldStream=True&suppress-autostart");
        Browser.Equal("Streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.Exists(By.Id("call-blazor-start"));

        var status = Browser.FindElement(By.Id("status"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("False", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.True(status.IsStale, TimeSpan.FromSeconds(10));

        Browser.Click(By.Id("stop-streaming-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
    }

    [Fact]
    public void BrowserConfigurationFromStreamingContentAppliesBeforeCircuitStarts()
    {
        using var fixture = CreateFixture($"--BrowserConfigurationLogLevel={BrowserLogLevel.Error}", "--BrowserConfigurationTraceOnStreamingCounter=true");
        Browser.Navigate(fixture.RootUri, $"{ServerPathBase}/streaming-interactivity?ShouldStream=True&suppress-autostart");
        Browser.Equal("Streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.Exists(By.Id("call-blazor-start"));

        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("False", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Click(By.Id("stop-streaming-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));
    }

    [Fact]
    public void BrowserConfigurationFromEnhancedNavigationStreamingContentAppliesBeforeCircuitStarts()
    {
        using var fixture = CreateFixture($"--BrowserConfigurationLogLevel={BrowserLogLevel.Error}", "--BrowserConfigurationTraceOnStreamingCounter=true");
        NavigateToStartPage(fixture);

        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("False", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Click(By.Id("stop-streaming-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));
    }

    [Fact]
    public void ReconnectionRetryIntervalControlsTransportAttempts()
    {
        using var fixture = CreateFixture("--BrowserConfigurationReconnectionMaxRetries=2", "--BrowserConfigurationReconnectionRetryIntervalMs=200");
        NavigateToStartPage(fixture);
        StartCounter();

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("""
            window.retryTimes = [];
            const originalFetch = window.fetch;
            window.fetch = (...args) => {
                if (String(args[0]).includes('/_blazor/negotiate')) {
                    window.retryTimes.push(performance.now());
                }
                return originalFetch(...args);
            };
            """);

        var driver = Assert.IsType<ChromeDriver>(Browser);
        driver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>());
        try
        {
            SetOffline(driver, true);
            javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");
            Browser.True(() => Convert.ToInt32(javascript.ExecuteScript("return window.retryTimes.length"), CultureInfo.InvariantCulture) == 2);

            var elapsed = Convert.ToDouble(javascript.ExecuteScript("return window.retryTimes[1] - window.retryTimes[0]"), CultureInfo.InvariantCulture);
            Assert.InRange(elapsed, 150, 700);
        }
        finally
        {
            SetOffline(driver, false);
        }
    }

    private static BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> CreateFixture(params string[] arguments)
    {
        var fixture = new BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>();
        fixture.AdditionalArguments.AddRange(arguments);
        return fixture;
    }

    private void NavigateToStartPage(BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> fixture, string additionalQuery = "")
    {
        Browser.Navigate(fixture.RootUri, $"{ServerPathBase}/streaming-interactivity?suppress-autostart{additionalQuery}");
        Browser.Equal("Not streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.Exists(By.Id("call-blazor-start"));
    }

    private void StartCounter()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
    }

    private static void SetOffline(ChromeDriver driver, bool offline)
    {
        driver.ExecuteCdpCommand("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = offline,
            ["latency"] = 0,
            ["downloadThroughput"] = -1,
            ["uploadThroughput"] = -1,
        });
    }
}
