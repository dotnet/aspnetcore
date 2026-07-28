// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace TestApp.E2E.Tests.Tests;

// Verifying prerendered content by holding blazor.web.js via ResourceLock.
[Collection(nameof(E2ECollection))]
public class PrerenderingTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;

    public PrerenderingTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>();
    }

    [Fact]
    public async Task HomePage_ShowsPrerenderContent_BeforeBlazorStarts()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var page = await context.NewPageAsync();

        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        await page.GotoAsync(_server.TestUrl, new() { WaitUntil = WaitUntilState.Commit });

        await blazorScript.WaitForRequestAsync();

        var heading = page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Hello, world!");

        var counterLink = page.Locator("a.nav-link", new() { HasText = "Counter" });
        await Expect(counterLink).ToBeVisibleAsync();

        await blazorScript.ReleaseAsync();

        await page.WaitForBlazorAsync();

        var enhancedNav = page.WaitForEnhancedNavigationAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await enhancedNav;
        await page.WaitForURLAsync("**/counter");

        await page.WaitForInteractiveAsync("button.btn-primary");

        var counterButton = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await counterButton.ClickAsync();
        var counterDisplay = page.Locator("p[role='status']");
        await Expect(counterDisplay).ToHaveTextAsync("Current count: 1");
    }

    [Fact]
    public async Task WeatherPage_ShowsLoadingState_BeforeBlazorStarts()
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        var page = await context.NewPageAsync();

        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        await page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Commit });

        await blazorScript.WaitForRequestAsync();

        var heading = page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Weather");

        await blazorScript.ReleaseAsync();

        var table = page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();
    }
}
