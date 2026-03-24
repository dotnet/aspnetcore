// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AsyncStateApp.Components;
using AsyncStateApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace AsyncStateApp.E2E.Tests.Tests;

// Detecting enhanced navigation (DOM patching) via the Blazor 'enhancedload' event.
//
// Zero app changes required. Blazor fires 'enhancedload' after an enhanced
// navigation patches the DOM. The WaitForEnhancedNavigationAsync() helper
// registers a one-shot listener before clicking a nav link, and the returned
// task completes when enhanced nav finishes (DOM patching, not full page reload).
[Collection(nameof(E2ECollection))]
public class EnhancedNavTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public EnhancedNavTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>();
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
    public async Task NavLink_TriggersEnhancedNavigation_ToCounterPage()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        // Register listener before clicking, await after
        var enhancedNav = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await enhancedNav;

        // Verify the counter page rendered via DOM patching
        var heading = _page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Counter");

        // Verify the URL updated without a full page reload
        Assert.Contains("/counter", _page.Url);
    }

    [Fact]
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

    [Fact]
    public async Task SequentialEnhancedNav_PatchesDOMMultipleTimes()
    {
        await _page.GotoAsync(_server.TestUrl);
        await _page.WaitForBlazorAsync();

        // Navigate Home → Counter via enhanced nav
        var nav1 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await nav1;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Counter");

        // Navigate Counter → Weather via enhanced nav
        var nav2 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await nav2;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Weather");

        // Navigate Weather → Home via enhanced nav
        var nav3 = _page.WaitForEnhancedNavigationAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await nav3;

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
