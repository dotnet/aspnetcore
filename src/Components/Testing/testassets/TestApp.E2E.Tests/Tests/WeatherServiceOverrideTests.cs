// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;
using TestApp.E2E.Tests.ServiceOverrides;

namespace TestApp.E2E.Tests.Tests;

// Tests that run against the app with the FakeWeather service override.
[TestClass]
public class WeatherServiceOverrideTests
{
    public TestContext TestContext { get; set; } = null!;

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>(options =>
        {
            options.ConfigureServices<TestOverrides>(nameof(TestOverrides.FakeWeather));
        });
        _context = await TestRoot.Browser.NewContextAsync(
            new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await _context.NewPageAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
        TestContext.AttachServerOutputIfFailed(_server);
    }

    [TestMethod]
    public async Task WeatherPage_ShowsFakeData()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        var table = _page.Locator("table.table");
        await Assertions.Expect(table).ToBeVisibleAsync();

        var summaryCell = table.Locator("td", new() { HasText = "TestWeather" });
        await Assertions.Expect(summaryCell).ToBeVisibleAsync();

        var tempCell = table.Locator("td", new() { HasText = "42" });
        await Assertions.Expect(tempCell).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task WeatherPage_ShowsExactlyOneRow()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        var table = _page.Locator("table.table");
        await Assertions.Expect(table).ToBeVisibleAsync();

        var rows = table.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(1);
    }
}
