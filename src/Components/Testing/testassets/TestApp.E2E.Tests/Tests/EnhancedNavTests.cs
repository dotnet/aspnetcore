// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

// Detecting enhanced navigation (DOM patching) via the Blazor 'enhancedload' event.
[TestClass]
public class EnhancedNavTests : BrowserTest
{
    private ServerInstance _server = null!;
    private IPage _page = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>();
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestCleanup]
    public void AttachServerOutput() => TestContext.AttachServerOutputIfFailed(_server);

    [TestMethod]
    public async Task NavLink_TriggersEnhancedNavigation_ToCounterPage()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        var enhancedNav = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await enhancedNav;

        var heading = _page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Counter");

        Assert.Contains("/counter", _page.Url);
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
        await Expect(heading).ToHaveTextAsync("Weather");

        Assert.Contains("/weather", _page.Url);
    }

    [TestMethod]
    public async Task SequentialEnhancedNav_PatchesDOMMultipleTimes()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        var nav1 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await nav1;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Counter");

        var nav2 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await nav2;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Weather");

        var nav3 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await nav3;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
