// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace TestApp.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class HomePageTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public HomePageTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task HomePage_DisplaysTitle()
    {
        await _page.GotoAsync(_server.TestUrl);

        await Expect(_page).ToHaveTitleAsync("Home");
    }

    [Fact]
    public async Task HomePage_HasHelloWorldHeading()
    {
        await _page.GotoAsync(_server.TestUrl);

        var heading = _page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Hello, world!");
    }

    [Fact]
    public async Task CounterPage_IncrementsOnClick()
    {
        await _page.GotoAsync($"{_server.TestUrl}/counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        // Wait for Blazor Server interactivity before clicking
        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }
}
