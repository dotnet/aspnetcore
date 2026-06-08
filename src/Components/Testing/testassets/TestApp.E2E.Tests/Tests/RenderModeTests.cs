// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

// Tests that validate E2E testing works across all Blazor render modes.
// - Static SSR: Content available immediately after navigation
// - Interactive Server: Must wait for SignalR circuit
// - Interactive WASM: Must wait for .NET runtime download + initialization
// - Interactive Auto: First load = Server, subsequent = WASM
[TestClass]
public class RenderModeTests
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
    public async Task StaticSSR_RendersContent()
    {
        await _page.GotoAsync($"{_server.TestUrl}/static-info");

        await Assertions.Expect(_page).ToHaveTitleAsync("Static Info");
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Static Info");

        var staticContent = _page.Locator("#static-content");
        await Assertions.Expect(staticContent).ToBeVisibleAsync();
        await Assertions.Expect(staticContent).ToContainTextAsync("Development");
    }

    [TestMethod]
    public async Task StaticSSR_WeatherPage_StreamRenders()
    {
        await _page.GotoAsync($"{_server.TestUrl}/weather");

        var table = _page.Locator("table.table");
        await Assertions.Expect(table).ToBeVisibleAsync();

        await Assertions.Expect(table.Locator("tbody tr")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task InteractiveServer_CounterWorks()
    {
        await _page.GotoAsync($"{_server.TestUrl}/server-counter");

        await Assertions.Expect(_page).ToHaveTitleAsync("Server Counter");
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Server Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Assertions.Expect(button).ToBeVisibleAsync();

        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Assertions.Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task InteractiveWebAssembly_CounterWorks()
    {
        await _page.GotoAsync($"{_server.TestUrl}/wasm-counter");

        await Assertions.Expect(_page).ToHaveTitleAsync("WASM Counter");
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("WASM Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Assertions.Expect(button).ToBeVisibleAsync();

        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Assertions.Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task InteractiveAuto_CounterWorks()
    {
        await _page.GotoAsync($"{_server.TestUrl}/auto-counter");

        await Assertions.Expect(_page).ToHaveTitleAsync("Auto Counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Assertions.Expect(button).ToBeVisibleAsync();

        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Assertions.Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task Navigation_AcrossRenderModes_Works()
    {
        await _page.GotoAsync($"{_server.TestUrl}/static-info");
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Static Info");

        await _page.GetByText("Server Counter").ClickAsync();
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Server Counter");

        await _page.GetByText("Auto Counter").ClickAsync();
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Auto Counter");

        await _page.GetByText("WASM Counter").ClickAsync();
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("WASM Counter");

        await _page.GetByText("Home").ClickAsync();
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
