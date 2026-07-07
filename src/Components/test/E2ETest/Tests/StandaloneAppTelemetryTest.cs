// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

/// <summary>
/// E2E tests validating that Blazor WASM telemetry (OTel traces/logs) flows from the browser
/// through the gateway to a backend collector, and that service discovery allows the WASM app
/// to call APIs via the gateway's reverse proxy.
/// </summary>
public class StandaloneAppTelemetryTest
    : ServerTestBase<StandaloneAppTelemetryFixture>
{
    public StandaloneAppTelemetryTest(
        BrowserFixture browserFixture,
        StandaloneAppTelemetryFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Browser.SetWindowSize(1920, 1080);
        Navigate("/");
        WaitUntilLoaded();
    }

    [Fact]
    public void FetchData_LoadsWeatherFromBackendApi_ViaServiceDiscovery()
    {
        // Navigate to the service discovery page
        Navigate("/servicediscoveryfetchdata");
        Assert.Equal("Weather forecast", Browser.Exists(By.TagName("h1")).Text);

        // Wait for the table to appear with data from the fake weather API
        var tableSelector = By.CssSelector("table.table");
        Browser.Exists(tableSelector);

        var rows = Browser.FindElements(By.CssSelector("table.table tbody tr"));
        Assert.True(rows.Count > 0, "Expected weather data rows but found none.");

        // Verify data from our fake weather API is displayed (contains "Warm")
        var pageText = Browser.FindElement(By.CssSelector("table.table")).Text;
        Assert.Contains("Warm", pageText);

        // Verify the fake weather API was actually hit
        Assert.True(_serverFixture.WeatherApiRequestCount > 0,
            "Weather API was never called — service discovery may not be working.");
    }

    [Fact]
    public async Task FetchData_SendsTraces_ToOtlpCollector()
    {
        // Navigate to service discovery page to trigger an HTTP call
        Navigate("/servicediscoveryfetchdata");
        Browser.Exists(By.CssSelector("table.table"));

        // Wait for trace exports to arrive at the fake collector.
        // The WASM app's BackgroundExportHandler fires requests asynchronously,
        // and the OTel batch processor has a default 5-second schedule delay.
        var received = await _serverFixture.WaitForTracesAsync(1, TimeSpan.FromSeconds(30));

        Assert.True(received,
            $"Expected at least 1 trace export to arrive at the fake collector. " +
            $"Traces: {_serverFixture.Traces.Count}, Logs: {_serverFixture.Logs.Count}. " +
            $"This may indicate the WASM OTel initialization failed or gateway OTLP forwarding is broken.");
    }

    [Fact]
    public async Task FetchData_SendsLogs_ToOtlpCollector()
    {
        // Navigate to service discovery page to trigger HttpClient logging
        Navigate("/servicediscoveryfetchdata");
        Browser.Exists(By.CssSelector("table.table"));

        // Wait for log exports to arrive at the fake collector
        var received = await _serverFixture.WaitForLogsAsync(1, TimeSpan.FromSeconds(30));

        Assert.True(received,
            $"Expected at least 1 log export to arrive at the fake collector. " +
            $"Logs: {_serverFixture.Logs.Count}. " +
            $"This may indicate the WASM OTel log exporter is not configured or gateway forwarding is broken.");
    }

    private void WaitUntilLoaded()
    {
        var app = Browser.Exists(By.TagName("app"));
        Browser.NotEqual("Loading...", () => app.Text);
    }
}
