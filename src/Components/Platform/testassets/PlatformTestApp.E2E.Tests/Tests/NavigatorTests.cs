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
public partial class NavigatorTests : BrowserTest
{
    private static readonly string[] NavigatorPaths = ["/navigator-server", "/navigator-wasm"];

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        // Values other than the defaults show that each one comes from the browser.
        var options = new BrowserNewContextOptions
        {
            Locale = "fr-CA",
            UserAgent = "PlatformTestApp.E2E.Tests",
            HasTouch = true,
        };
        _context = await NewContext(options.WithServerRouting(_server));
        _page = await _context.NewPageAsync();
    }

    [TestMethod]
    public async Task Navigator_ReadsBrowserValuesInBothRenderModes()
    {
        foreach (var path in NavigatorPaths)
        {
            await NavigateAsync(path, "#get-language");

            await RunAsync("#get-language", "Language: fr-CA");
            await RunAsync("#get-languages", $"Languages: {string.Join(", ", await EvaluateAsync<string[]>("navigator.languages"))}");
            await RunAsync("#get-online", "Online: True");
            await RunAsync("#get-cookie-enabled", "Cookie enabled: True");
            await RunAsync("#get-user-agent", "User agent: PlatformTestApp.E2E.Tests");
            await RunAsync("#get-hardware-concurrency", $"Hardware concurrency: {await EvaluateAsync<int>("navigator.hardwareConcurrency")}");
            await RunAsync("#get-max-touch-points", $"Max touch points: {await EvaluateAsync<int>("navigator.maxTouchPoints")}");
        }

        Assert.AreNotEqual(0, await EvaluateAsync<int>("navigator.maxTouchPoints"));
    }

    [TestMethod]
    public async Task Navigator_ReadsTheCurrentOnlineState()
    {
        // Going offline would also cut the Interactive Server circuit, so this runs in WebAssembly only.
        await NavigateAsync("/navigator-wasm", "#get-online");

        await _context.SetOfflineAsync(true);
        await RunAsync("#get-online", "Online: False");

        await _context.SetOfflineAsync(false);
        await RunAsync("#get-online", "Online: True");
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private Task<T> EvaluateAsync<T>(string expression) => _page.EvaluateAsync<T>(expression);

    private async Task RunAsync(string buttonSelector, string expectedResult)
    {
        var result = _page.Locator("#navigator-result");
        // Waiting for the next run keeps a step from passing on the text of the previous step.
        var nextRun = int.Parse(await result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await Expect(result).ToHaveTextAsync(expectedResult);
    }
}
