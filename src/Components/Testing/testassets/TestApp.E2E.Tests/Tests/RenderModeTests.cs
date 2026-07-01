// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace TestApp.E2E.Tests.Tests;

// Tests that validate E2E testing works across all Blazor render modes.
// - Static SSR: Content available immediately after navigation
// - Interactive Server: Must wait for SignalR circuit
// - Interactive WASM: Must wait for .NET runtime download + initialization
// - Interactive Auto: First load = Server, subsequent = WASM
[Collection(nameof(E2ECollection))]
public class RenderModeTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;
    private IPage _page = null!;

    public RenderModeTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task StaticSSR_RendersContent()
    {
        await _page.GotoAsync($"{_server.TestUrl}/static-info");

        await Expect(_page).ToHaveTitleAsync("Static Info");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Static Info");

        var staticContent = _page.Locator("#static-content");
        await Expect(staticContent).ToBeVisibleAsync();
        await Expect(staticContent).ToContainTextAsync("Development");
    }

    [Fact]
    public async Task StaticSSR_WeatherPage_StreamRenders()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        // Weather page uses [StreamRendering] — initially shows "Loading..."
        // then streams the table after the async delay.
        var table = _page.Locator("table.table");
        await Expect(table).ToBeVisibleAsync();

        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task InteractiveServer_CounterWorks()
    {
        await _page.GotoAsync($"{_server.TestUrl}/server-counter");

        await Expect(_page).ToHaveTitleAsync("Server Counter");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Server Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        // Wait for Blazor Server interactivity before clicking
        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [Fact]
    public async Task InteractiveWebAssembly_CounterWorks()
    {
        await _page.GotoAsync($"{_server.TestUrl}/wasm-counter");

        await Expect(_page).ToHaveTitleAsync("WASM Counter");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("WASM Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        // Wait for WASM interactivity (downloading .NET runtime takes longer)
        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [Fact]
    public async Task InteractiveAuto_CounterWorks()
    {
        // Auto mode: first load uses Server (via SignalR), subsequent loads
        // use WASM (after runtime assets are cached).
        await _page.GotoAsync($"{_server.TestUrl}/auto-counter");

        await Expect(_page).ToHaveTitleAsync("Auto Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [Fact]
    public async Task Navigation_AcrossRenderModes_Works()
    {
        // Start at Static SSR page
        await _page.GotoAsync($"{_server.TestUrl}/static-info");
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Static Info");

        // Navigate to Interactive Server page
        await _page.GetByText("Server Counter").ClickAsync();
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Server Counter");

        // Navigate to Interactive Auto page
        await _page.GetByText("Auto Counter").ClickAsync();
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Auto Counter");

        // Navigate to Interactive WASM page
        await _page.GetByText("WASM Counter").ClickAsync();
        await Expect(_page.Locator("h1")).ToHaveTextAsync("WASM Counter");

        // Navigate back to Home (Static SSR)
        await _page.GetByText("Home").ClickAsync();
        await Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
