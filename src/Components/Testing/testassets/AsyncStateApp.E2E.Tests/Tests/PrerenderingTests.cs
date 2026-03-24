// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using AsyncStateApp.Components;
using AsyncStateApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace AsyncStateApp.E2E.Tests.Tests;

// Verifying prerendered content by holding blazor.web.js via RouteAsync.
//
// Zero app changes required. The test intercepts the Blazor script request
// at the Playwright network layer, holds it via a TCS, verifies SSR content
// is visible while Blazor hasn't started, then releases the script and
// verifies interactivity kicks in.
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

        // Hold blazor.web.js via ResourceLock. Uses regex to match fingerprinted
        // URLs: blazor.web.js becomes blazor.web.<hash>.js via @Assets in .NET 10
        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        // Navigate to home page — the server will return prerendered HTML
        // but Blazor won't start because the script is held
        await page.GotoAsync(_server.TestUrl, new() { WaitUntil = WaitUntilState.Commit });

        // Wait until the browser actually requests blazor.web.js
        await blazorScript.WaitForRequestAsync();

        // Verify SSR content is visible while Blazor hasn't started
        var heading = page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Hello, world!");

        // The counter page link should be present in SSR (static nav)
        var counterLink = page.Locator("a.nav-link", new() { HasText = "Counter" });
        await Expect(counterLink).ToBeVisibleAsync();

        // Release the script so Blazor can start
        await blazorScript.ReleaseAsync();

        // Wait for Blazor to fully initialize
        await page.WaitForBlazorAsync();

        // Navigate to counter via enhanced navigation
        var enhancedNav = page.WaitForEnhancedNavigationAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await enhancedNav;
        await page.WaitForURLAsync("**/counter");

        // Wait for the counter button to become interactive
        await page.WaitForInteractiveAsync("button.btn-primary");

        // Component is interactive — click the counter and verify it increments
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

        // Hold blazor.web.js via ResourceLock
        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        // Navigate directly to weather page — SSR will render the loading state
        // because OnInitializedAsync hasn't completed in the prerender
        await page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Commit });

        await blazorScript.WaitForRequestAsync();

        // The "Weather" heading should be prerendered
        var heading = page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Weather");

        // Release Blazor
        await blazorScript.ReleaseAsync();

        // After Blazor starts and streaming completes, the table should appear
        var table = page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();
    }
}
