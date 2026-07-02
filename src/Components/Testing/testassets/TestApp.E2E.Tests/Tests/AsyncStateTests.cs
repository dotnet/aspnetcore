// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;
using TestApp.E2E.Tests.ServiceOverrides;

namespace TestApp.E2E.Tests.Tests;

// Deterministic async state control via TestLockClient.
[TestClass]
public class AsyncStateTests : BrowserTest
{
    private ServerInstance _server = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>(options =>
        {
            options.ConfigureServices<TestOverrides>(nameof(TestOverrides.LockableWeather));
        });
    }

    [TestCleanup]
    public void AttachServerOutput() => TestContext.AttachServerOutputIfFailed(_server);

    [TestMethod]
    public async Task WeatherPage_ShowsLoadingThenData_WhenLockReleased()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var locks = await TestLockClient.CreateAsync(_server, context);
        var page = await context.NewPageAsync();

        var navigationTask = page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Load });

        await using (locks.Lock("weather-data"))
        {
            var loading = page.Locator("p em", new() { HasText = "Loading..." });
            await Expect(loading).ToBeVisibleAsync();

            var table = page.Locator("table.table");
            await Expect(table).Not.ToBeVisibleAsync();
        }

        await navigationTask;

        var dataTable = page.Locator("table.table");
        await Expect(dataTable).ToBeVisibleAsync();

        var sunnyCell = dataTable.Locator("td", new() { HasText = "TestSunny" });
        await Expect(sunnyCell).ToBeVisibleAsync();

        var cloudyCell = dataTable.Locator("td", new() { HasText = "TestCloudy" });
        await Expect(cloudyCell).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task WeatherPage_ShowsExactRowCount_AfterLockRelease()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var locks = await TestLockClient.CreateAsync(_server, context);
        var page = await context.NewPageAsync();

        var navigationTask = page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Load });

        await using (locks.Lock("weather-data"))
        {
            var loading = page.Locator("p em", new() { HasText = "Loading..." });
            await Expect(loading).ToBeVisibleAsync();
        }

        await navigationTask;

        var table = page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();

        var rows = table.Locator("tbody tr");
        await Expect(rows).ToHaveCountAsync(2);
    }
}
