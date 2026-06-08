// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

[TestClass]
public class HomePageTests
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
    public async Task HomePage_DisplaysTitle()
    {
        await _page.GotoAsync(_server.TestUrl);

        await Assertions.Expect(_page).ToHaveTitleAsync("Home");
    }

    [TestMethod]
    public async Task HomePage_HasHelloWorldHeading()
    {
        await _page.GotoAsync(_server.TestUrl);

        var heading = _page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task CounterPage_IncrementsOnClick()
    {
        await _page.GotoAsync($"{_server.TestUrl}/counter");

        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Assertions.Expect(button).ToBeVisibleAsync();

        await _page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = _page.Locator("p[role='status']");
        await Assertions.Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }
}
