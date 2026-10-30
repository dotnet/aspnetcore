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
