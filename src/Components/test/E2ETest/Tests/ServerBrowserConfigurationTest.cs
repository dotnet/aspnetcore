// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

    [Theory]
    [InlineData(3000, null)]
    [InlineData(3000, 500)]
    [InlineData(500, 3000)]
    [InlineData(3000, 0)]
    public void CircuitInactivityTimeoutAppliesAfterRemovingLastServerComponent(int serverTimeout, int? javaScriptTimeout)
    {
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
        StartCounter(javaScriptTimeout is { } timeout ? $$"""{ ssr: { circuitInactivityTimeoutMs: {{timeout}} } }""" : "{}");

        javascript.ExecuteScript("window.componentRemovedAt = performance.now()");
        Browser.Click(By.Id("remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("increment-0"));
        Browser.True(() => (bool)javascript.ExecuteScript("return window.socketClosedAt !== undefined"));

        var elapsed = Convert.ToDouble(javascript.ExecuteScript("return window.socketClosedAt - window.componentRemovedAt"), CultureInfo.InvariantCulture);
        var expectedTimeout = javaScriptTimeout ?? serverTimeout;
        Assert.InRange(elapsed, Math.Max(0, expectedTimeout - 500), expectedTimeout + 1500);
    }

    [Theory]
    [InlineData(BrowserLogLevel.Trace, null, true)]
    [InlineData(BrowserLogLevel.Trace, BrowserLogLevel.Error, false)]
    [InlineData(BrowserLogLevel.Error, BrowserLogLevel.Trace, true)]
    public void CircuitLogLevelControlsEventMessages(BrowserLogLevel serverLogLevel, BrowserLogLevel? javaScriptLogLevel, bool expectEventMessages)
    {
        using var fixture = CreateFixture($"--BrowserConfigurationLogLevel={serverLogLevel}");
        NavigateToStartPage(fixture);
        StartCounter(javaScriptLogLevel is { } logLevel ? $$"""{ circuit: { logLevel: {{(int)logLevel}} } }""" : "{}");
        Browser.Manage().Logs.GetLog(LogType.Browser);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.Exists(By.Id("count-0")).Text);

        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.Equal(expectEventMessages, entries.Any(entry => entry.Message.Contains("Dispatching event with handler id", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(200, null)]
    [InlineData(null, "200")]
    [InlineData(1000, "200")]
    [InlineData(1000, "() => 200")]
    public void ReconnectionRetryIntervalControlsTransportAttempts(int? serverRetryInterval, string javaScriptRetryInterval)
    {
        using var fixture = CreateFixture("--BrowserConfigurationReconnectionMaxRetries=2");
        if (serverRetryInterval is { } retryInterval)
        {
            fixture.AdditionalArguments.Add($"--BrowserConfigurationReconnectionRetryIntervalMs={retryInterval}");
        }
        NavigateToStartPage(fixture);
        StartCounter(javaScriptRetryInterval is not null ? $$"""{ circuit: { reconnectionOptions: { retryIntervalMilliseconds: {{javaScriptRetryInterval}} } } }""" : "{}");

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

    private void StartCounter(string options = "{}")
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript($"Blazor.start({options})");
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
