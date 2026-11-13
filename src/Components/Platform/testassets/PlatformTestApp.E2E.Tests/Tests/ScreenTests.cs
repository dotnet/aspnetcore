// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class ScreenTests : BrowserTest
{
    private static readonly string[] ScreenPaths = ["/screen-server", "/screen-wasm"];

    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        // A screen size other than the default shows that the values come from the browser.
        // Playwright applies ScreenSize only together with a viewport.
        var options = new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 800, Height = 500 },
            ScreenSize = new() { Width = 1234, Height = 567 },
        };
        var context = await NewContext(options.WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task Screen_ReadsBrowserValuesInBothRenderModes()
    {
        foreach (var path in ScreenPaths)
        {
            await NavigateAsync(path, "#get-width");

            await RunAsync("#get-avail-width", $"Available width: {await EvaluateAsync("screen.availWidth")}");
            await RunAsync("#get-avail-height", $"Available height: {await EvaluateAsync("screen.availHeight")}");
            await RunAsync("#get-width", "Width: 1234");
            await RunAsync("#get-height", "Height: 567");
            await RunAsync("#get-color-depth", $"Color depth: {await EvaluateAsync("screen.colorDepth")}");
            await RunAsync("#get-pixel-depth", $"Pixel depth: {await EvaluateAsync("screen.pixelDepth")}");
        }
    }

    [TestMethod]
    public async Task ScreenOrientation_ReadsBrowserValuesInBothRenderModes()
    {
        foreach (var path in ScreenPaths)
        {
            await NavigateAsync(path, "#get-orientation-type");

            await RunAsync("#get-orientation-type", $"Orientation type: {await _page.EvaluateAsync<string>("screen.orientation.type")}, known: True");
            await RunAsync("#get-orientation-angle", $"Orientation angle: {await EvaluateAsync("screen.orientation.angle")}");
        }
    }

    [TestMethod]
    public async Task ScreenOrientation_ReportsChangeEventsInBothRenderModes()
    {
        foreach (var path in ScreenPaths)
        {
            await NavigateAsync(path, "#watch-orientation");
            var orientationEvent = _page.Locator("#orientation-event");

            await RunAsync("#watch-orientation", "Watch orientation: added");
            // Events that the page's own JavaScript dispatches reach the listener too.
            await _page.EvaluateAsync<bool>("screen.orientation.dispatchEvent(new Event('change'))");
            await Expect(orientationEvent).ToHaveTextAsync("change (1)");

            await RunAsync("#unwatch-orientation", "Stop watching orientation: removed");
            await _page.EvaluateAsync<bool>("screen.orientation.dispatchEvent(new Event('change'))");
            // A round trip after the browser event lets a late notification arrive before the check.
            await RunAsync("#get-orientation-angle", $"Orientation angle: {await EvaluateAsync("screen.orientation.angle")}");
            await Expect(orientationEvent).ToHaveTextAsync("change (1)");
        }
    }

    [TestMethod]
    public async Task ScreenOrientation_ReportsRotationInBothRenderModes()
    {
        // Playwright emulates a rotated screen only for a mobile viewport, which Firefox does not support.
        if (BrowserName != "chromium")
        {
            Assert.Inconclusive("Only Chromium emulates the screen orientation.");
        }

        var options = new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 400, Height = 700 },
            IsMobile = true,
        };
        var context = await NewContext(options.WithServerRouting(_server));
        _page = await context.NewPageAsync();

        foreach (var path in ScreenPaths)
        {
            await _page.SetViewportSizeAsync(400, 700);
            await NavigateAsync(path, "#watch-orientation");
            var orientationEvent = _page.Locator("#orientation-event");
            await RunAsync("#get-orientation-type", "Orientation type: portrait-primary, known: True");
            await RunAsync("#watch-orientation", "Watch orientation: added");

            await _page.SetViewportSizeAsync(700, 400);

            await Expect(orientationEvent).ToHaveTextAsync("change (1)");
            await RunAsync("#get-orientation-type", "Orientation type: landscape-primary, known: True");
            await RunAsync("#get-orientation-angle", "Orientation angle: 90");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task<string> EvaluateAsync(string expression)
        => (await _page.EvaluateAsync<int>(expression)).ToString(CultureInfo.InvariantCulture);

    private async Task RunAsync(string buttonSelector, string expectedResult)
    {
        var result = _page.Locator("#screen-result");
        // Waiting for the next run keeps a step from passing on the text of the previous step.
        var nextRun = int.Parse(await result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await Expect(result).ToHaveTextAsync(expectedResult);
    }
}
