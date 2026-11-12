// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
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
        using var fixture = CreateFixture("--BrowserConfigurationCircuitInactivityTimeoutMs=3000");
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
        Assert.InRange(elapsed, 2500, 10000);
    }

    [Fact]
    public void DisablingPreserveDomUsesFullPageNavigation()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture);
        StartCounter();
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.originalDocument = document");

        Browser.Click(By.Id("add-server-counter-prerendered-link"));
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-1")).Text);

        Browser.False(() => (bool)javascript.ExecuteScript("return window.originalDocument === document"));
        Browser.Click(By.Id("increment-1"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-1")).Text);
    }

    [Fact]
    public async Task DisablingPreserveDomReplacesElementsDuringStreamingUpdates()
    {
        using var fixture = CreateFixture("--BrowserConfigurationPreserveDom=false");
        NavigateToStartPage(fixture);
        StartCounter();
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.Exists(By.Id("status")).Text);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("window.originalStatus = document.getElementById('status')");
        using var httpClient = new HttpClient();
        try
        {
            var updateUri = Browser.Exists(By.Id("add-server-counter-prerendered-link")).GetAttribute("href");
            using var response = await httpClient.GetAsync(updateUri);
            response.EnsureSuccessStatusCode();
            Browser.Exists(By.Id("count-1"));

            Browser.False(() => (bool)javascript.ExecuteScript("return window.originalStatus === document.getElementById('status')"));
        }
        finally
        {
            var endUri = Browser.Exists(By.Id("stop-streaming-link")).GetAttribute("href");
            using var response = await httpClient.GetAsync(endUri);
            response.EnsureSuccessStatusCode();
        }
        Browser.Equal("Not streaming", () => Browser.Exists(By.Id("status")).Text);
        Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-1")).Text);
    }

    [Fact]
    public void TraceLogLevelIncludesCircuitEventMessages()
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
    public void ReconnectionRetryIntervalControlsTransportAttempts()
    {
        using var fixture = CreateFixture(
            "--BrowserConfigurationReconnectionMaxRetries=2",
            "--BrowserConfigurationReconnectionRetryIntervalMs=200");
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

    private void NavigateToStartPage(BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> fixture)
    {
        Browser.Navigate(fixture.RootUri, $"{ServerPathBase}/streaming-interactivity?suppress-autostart");
        Browser.Equal("Not streaming", () => Browser.Exists(By.Id("status")).Text);
    }

    private void StartCounter()
    {
        Browser.Click(By.Id("call-blazor-start"));
        Browser.Exists(By.Id("blazor-started"));
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
