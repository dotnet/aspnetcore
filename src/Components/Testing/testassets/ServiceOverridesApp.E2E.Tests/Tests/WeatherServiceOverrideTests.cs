// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using ServiceOverridesApp.Components;
using ServiceOverridesApp.E2E.Tests.Fixtures;
using ServiceOverridesApp.E2E.Tests.ServiceOverrides;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace ServiceOverridesApp.E2E.Tests.Tests;

// Tests that run against the app with the FakeWeather service override.
// The weather service is replaced with a deterministic fake via
// the static method pattern (WAF-like ConfigureTestServices).
[Collection(nameof(E2ECollection))]
public class WeatherServiceOverrideTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public WeatherServiceOverrideTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>(options =>
        {
            options.ConfigureServices<TestOverrides>(nameof(TestOverrides.FakeWeather));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
    public async Task WeatherPage_ShowsFakeData()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        // Wait for the weather data to load (streaming rendering)
        var table = _page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();

        // Verify the fake weather service data is displayed
        var summaryCell = table.Locator("td", new() { HasText = "TestWeather" });
        await Expect(summaryCell).ToBeVisibleAsync();

        // Verify the fake temperature (42°C)
        var tempCell = table.Locator("td", new() { HasText = "42" });
        await Expect(tempCell).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WeatherPage_ShowsExactlyOneRow()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        var table = _page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();

        // FakeWeatherService returns exactly one forecast
        var rows = table.Locator("tbody tr");
        await Expect(rows).ToHaveCountAsync(1);
    }
}
