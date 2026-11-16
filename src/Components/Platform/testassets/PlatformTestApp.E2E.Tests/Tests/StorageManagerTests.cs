// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class StorageManagerTests : BrowserTest
{
    private static readonly string[] StorageManagerPaths = ["/storagemanager-server", "/storagemanager-wasm"];

    private ServerInstance _server = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        _context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await _context.NewPageAsync();
    }

    [TestMethod]
    public async Task StorageManager_ReportsPersistenceAndEstimateInBothRenderModes()
    {
        foreach (var path in StorageManagerPaths)
        {
            await NavigateAsync(path, "#get-persisted");

            // Whether the browser grants persistence depends on its own heuristics, so the result is compared with the browser after each run.
            await RunAsync("#get-persisted", async result => await Expect(result).ToHaveTextAsync($"Persisted: {await PersistedInBrowserAsync()}"));
            await RunAsync("#persist", async result => await Expect(result).ToHaveTextAsync($"Persist: {await PersistedInBrowserAsync()}"));

            // "null" would show a member that did not come back from the browser.
            await RunAsync("#estimate", new Regex(@"^Estimate: usage=\d+, quota=[1-9]\d*$"));
        }
    }

    [TestMethod]
    public async Task StorageManager_ThrowsNotSupportedExceptionWithoutTheBrowserApi()
    {
        // Same as a page that is not a secure context. The context belongs to this test only.
        await _context.AddInitScriptAsync("delete Navigator.prototype.storage;");

        foreach (var path in StorageManagerPaths)
        {
            await NavigateAsync(path, "#get-persisted");
            Assert.IsFalse(await _page.EvaluateAsync<bool>("'storage' in navigator"));

            await RunAsync("#get-persisted", "Persisted: Not supported: JSException");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }

    private async Task<string> PersistedInBrowserAsync()
        => await _page.EvaluateAsync<bool>("navigator.storage.persisted()") ? "True" : "False";

    private Task RunAsync(string buttonSelector, string expectedResult)
        => RunAsync(buttonSelector, result => Expect(result).ToHaveTextAsync(expectedResult));

    private Task RunAsync(string buttonSelector, Regex expectedResult)
        => RunAsync(buttonSelector, result => Expect(result).ToHaveTextAsync(expectedResult));

    private async Task RunAsync(string buttonSelector, Func<ILocator, Task> assertResult)
    {
        var result = _page.Locator("#storage-manager-result");
        // Waiting for the next run keeps a step from passing on the text of the previous step.
        var nextRun = int.Parse(await result.GetAttributeAsync("data-run") ?? "", CultureInfo.InvariantCulture) + 1;

        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-run", nextRun.ToString(CultureInfo.InvariantCulture));
        await assertResult(result);
    }
}
