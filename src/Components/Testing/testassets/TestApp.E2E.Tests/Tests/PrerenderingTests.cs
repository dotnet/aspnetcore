// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

// Verifying prerendered content by holding blazor.web.js via ResourceLock.
[TestClass]
public class PrerenderingTests
{
    public TestContext TestContext { get; set; } = null!;

    private ServerInstance _server = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestContext.AttachServerOutputIfFailed(_server);
    }

    [TestMethod]
    public async Task HomePage_ShowsPrerenderContent_BeforeBlazorStarts()
    {
        var context = await TestRoot.Browser.NewContextAsync(
            new BrowserNewContextOptions().WithServerRouting(_server));
        await using var contextScope = context;
        var page = await context.NewPageAsync();

        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        await page.GotoAsync(_server.TestUrl, new() { WaitUntil = WaitUntilState.Commit });

        await blazorScript.WaitForRequestAsync();

        var heading = page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Hello, world!");

        var counterLink = page.Locator("a.nav-link", new() { HasText = "Counter" });
        await Assertions.Expect(counterLink).ToBeVisibleAsync();

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
        await Assertions.Expect(counterDisplay).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task WeatherPage_ShowsLoadingState_BeforeBlazorStarts()
    {
        var context = await TestRoot.Browser.NewContextAsync(
            new BrowserNewContextOptions().WithServerRouting(_server));
        await using var contextScope = context;
        var page = await context.NewPageAsync();

        await using var blazorScript = await ResourceLock.CreateAsync(
            page, new Regex("blazor\\.web.*\\.js"));

        await page.GotoAsync($"{_server.TestUrl}/weather",
            new() { WaitUntil = WaitUntilState.Commit });

        await blazorScript.WaitForRequestAsync();

        var heading = page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Weather");

        await blazorScript.ReleaseAsync();

        var table = page.Locator("table.table");
        await Assertions.Expect(table).ToBeVisibleAsync();
    }
}
