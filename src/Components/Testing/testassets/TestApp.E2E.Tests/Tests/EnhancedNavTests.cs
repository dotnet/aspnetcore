// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

// Detecting enhanced navigation (DOM patching) via the Blazor 'enhancedload' event.
[TestClass]
public class EnhancedNavTests
{
    public TestContext TestContext { get; set; } = null!;

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>();
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
    public async Task NavLink_TriggersEnhancedNavigation_ToCounterPage()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        var enhancedNav = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await enhancedNav;

        var heading = _page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Counter");

        StringAssert.Contains(_page.Url, "/counter");
    }

    [TestMethod]
    public async Task NavLink_TriggersEnhancedNavigation_ToWeatherPage()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        var enhancedNav = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await enhancedNav;

        var heading = _page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Weather");

        StringAssert.Contains(_page.Url, "/weather");
    }

    [TestMethod]
    public async Task SequentialEnhancedNav_PatchesDOMMultipleTimes()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        var nav1 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await nav1;

        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Counter");

        var nav2 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await nav2;

        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Weather");

        var nav3 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await nav3;

        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
