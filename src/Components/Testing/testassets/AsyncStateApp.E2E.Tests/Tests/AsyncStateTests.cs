// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AsyncStateApp.Components;
using AsyncStateApp.E2E.Tests.Fixtures;
using AsyncStateApp.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace AsyncStateApp.E2E.Tests.Tests;

// Deterministic async state control via TestLockClient.
//
// The app's weather service is replaced with LockableWeatherService (via service
// override) that blocks until the test releases a lock. TestLockClient abstracts
// away session IDs, cookies, and HTTP calls — the test just creates a lock and
// disposes it when ready for data to flow.
[Collection(nameof(E2ECollection))]
public class AsyncStateTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;

    public AsyncStateTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>(options =>
        {
            options.ConfigureServices<TestOverrides>(nameof(TestOverrides.LockableWeather));
        });
    }

    [Fact]
    public async Task WeatherPage_ShowsLoadingThenData_WhenLockReleased()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var locks = await TestLockClient.CreateAsync(_server, context);
        var page = await context.NewPageAsync();

        // Start navigation but don't await — the streaming response stays open
        // while the lock blocks, so we need to release the lock before the
        // navigation completes.
        var navigationTask = page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Load });

        // Hold the lock while we verify the loading state
        await using (locks.Lock("weather-data"))
        {
            // Verify the loading state is visible (streaming hasn't completed)
            var loading = page.Locator("p em", new() { HasText = "Loading..." });
            await Expect(loading).ToBeVisibleAsync();

            // The data table should NOT be visible yet
            var table = page.Locator("table.table");
            await Expect(table).Not.ToBeVisibleAsync();
        }
        // Lock released — data flows

        // Now await the navigation — streaming should complete after lock release
        await navigationTask;

        // Now the data should stream in
        var dataTable = page.Locator("table.table");
        await Expect(dataTable).ToBeVisibleAsync();

        // Verify deterministic test data from LockableWeatherService
        var sunnyCell = dataTable.Locator("td", new() { HasText = "TestSunny" });
        await Expect(sunnyCell).ToBeVisibleAsync();

        var cloudyCell = dataTable.Locator("td", new() { HasText = "TestCloudy" });
        await Expect(cloudyCell).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WeatherPage_ShowsExactRowCount_AfterLockRelease()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var locks = await TestLockClient.CreateAsync(_server, context);
        var page = await context.NewPageAsync();

        // Start navigation without awaiting — release lock before streaming completes
        var navigationTask = page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Load });

        // Hold lock, verify loading state, then release
        await using (locks.Lock("weather-data"))
        {
            var loading = page.Locator("p em", new() { HasText = "Loading..." });
            await Expect(loading).ToBeVisibleAsync();
        }

        await navigationTask;

        var table = page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();

        // LockableWeatherService returns exactly 2 forecasts
        var rows = table.Locator("tbody tr");
        await Expect(rows).ToHaveCountAsync(2);
    }
}
