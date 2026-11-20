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
    ServerBrowserConfigurationTest.CircuitOptionsFixture serverFixture,
    ServerBrowserConfigurationTest.DomPreservationFixture domPreservationFixture,
    ServerBrowserConfigurationTest.NavigationFixture navigationFixture,
    ITestOutputHelper output)
    : ServerTestBase<ServerBrowserConfigurationTest.CircuitOptionsFixture>(browserFixture, serverFixture, output),
    IClassFixture<ServerBrowserConfigurationTest.DomPreservationFixture>,
    IClassFixture<ServerBrowserConfigurationTest.NavigationFixture>
{
    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CircuitInactivityTimeoutAppliesAfterRemovingLastServerComponent()
    {
        const int serverTimeout = 3000;
        NavigateToStartPage(_serverFixture);

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
    public void InitializerOptionsAreNotOverwrittenByNavigation()
    {
        const int initializerTimeout = 500;
        NavigateToStartPage(domPreservationFixture, "&initializer-inactivity-timeout&initializer-circuit-log-trace&initializer-preserve-dom");

        var initialDocument = Browser.FindElement(By.TagName("html"));
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
        javascript.ExecuteScript("Blazor.start({})");
        Browser.True(() => (bool)javascript.ExecuteScript("return window.inactivityTimeoutInitializerRan === true"));
        Browser.True(() => (bool)javascript.ExecuteScript("return window.circuitLogInitializerRan === true"));
        Browser.True(() => (bool)javascript.ExecuteScript("return window.preserveDomInitializerRan === true"));
        javascript.ExecuteScript("Blazor.navigateTo(document.getElementById('add-server-counter-prerendered-link').href)");
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Assert.False(initialDocument.IsStale());
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));

        javascript.ExecuteScript("window.componentRemovedAt = performance.now()");
        Browser.Click(By.Id("remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("increment-0"));
        Browser.True(() => (bool)javascript.ExecuteScript("return window.socketClosedAt !== undefined"));

        var elapsed = Convert.ToDouble(javascript.ExecuteScript("return window.socketClosedAt - window.componentRemovedAt"), CultureInfo.InvariantCulture);
        Assert.InRange(elapsed, 0, initializerTimeout + 1500);
    }

    [Fact]
    public void CircuitLogLevelControlsEventMessages()
    {
        NavigateToStartPage(_serverFixture);
        StartCounter();
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);

        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Contains(entries, entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal));
    }

    [Fact]
    public void PreserveDomDisabledReloadsFirstNavigationAndBack()
    {
        NavigateToStartPage(domPreservationFixture);

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
        NavigateToStartPage(domPreservationFixture);

        using var client = new HttpClient { BaseAddress = domPreservationFixture.RootUri };
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
        NavigateToStartPage(navigationFixture);

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
        NavigateToStartPage(domPreservationFixture);

        var initialDocument = Browser.FindElement(By.TagName("html"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({ ssr: { disableDomPreservation: false } })");
        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-0")).Text);
        Assert.False(initialDocument.IsStale());
    }

    [Fact]
    public async Task PreserveDomDisabledOnFormPostDoesNotReplayTheSubmission()
    {
        var operationId = Guid.NewGuid().ToString("N");
        Browser.Navigate(navigationFixture.RootUri, $"{ServerPathBase}/forms/streaming-rendering/{operationId}?suppress-autostart");
        Browser.Exists(By.Id("send"));
        Browser.Exists(By.Id("call-blazor-start"));

        var initialBody = Browser.FindElement(By.TagName("body"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({})");
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-form")).Text);
        using var client = new HttpClient { BaseAddress = navigationFixture.RootUri };
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
        Browser.Navigate(domPreservationFixture.RootUri, $"{ServerPathBase}/streaming-interactivity?ShouldStream=True&suppress-autostart");
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
        Browser.Navigate(domPreservationFixture.RootUri, $"{ServerPathBase}/streaming-interactivity?ShouldStream=True&suppress-autostart");
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
        NavigateToStartPage(domPreservationFixture);

        var initialDocument = Browser.FindElement(By.TagName("html"));
        ((IJavaScriptExecutor)Browser).ExecuteScript("Blazor.start({ ssr: { disableDomPreservation: false } })");
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.Exists(By.Id("status")).Text);
        Assert.False(initialDocument.IsStale());
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconnectionRetryIntervalControlsTransportAttempts(bool useInitializer)
    {
        NavigateToStartPage(_serverFixture, useInitializer ? "&initializer-retry-interval" : "");
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
            if (useInitializer)
            {
                Browser.True(() => (bool)javascript.ExecuteScript("return window.retryIntervalInitializerRan === true"));
            }

            var elapsed = Convert.ToDouble(javascript.ExecuteScript("return window.retryTimes[1] - window.retryTimes[0]"), CultureInfo.InvariantCulture);
            Assert.InRange(elapsed, 150, 700);
        }
        finally
        {
            SetOffline(driver, false);
        }
    }

    public class CircuitOptionsFixture : BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>
    {
        public CircuitOptionsFixture()
        {
            AdditionalArguments.AddRange([
                "--BrowserConfigurationCircuitInactivityTimeoutMs=3000",
                $"--BrowserConfigurationLogLevel={BrowserLogLevel.Trace}",
                "--BrowserConfigurationReconnectionMaxRetries=2",
                "--BrowserConfigurationReconnectionRetryIntervalMs=200",
            ]);
        }
    }

    public class DomPreservationFixture : BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>
    {
        public DomPreservationFixture()
        {
            AdditionalArguments.AddRange([
                "--BrowserConfigurationPreserveDom=false",
                $"--BrowserConfigurationLogLevel={BrowserLogLevel.Error}",
                "--BrowserConfigurationCircuitInactivityTimeoutMs=5000",
                "--BrowserConfigurationTraceOnStreamingCounter=true",
            ]);
        }
    }

    public class NavigationFixture : BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>
    {
        public NavigationFixture()
        {
            AdditionalArguments.AddRange([
                "--BrowserConfigurationDisableOnCounter=true",
                "--BrowserConfigurationDisableOnFormPost=true",
            ]);
        }
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
