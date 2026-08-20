// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformTestApp.Components;
using PlatformTestApp.E2E.Tests.Fixtures;

namespace PlatformTestApp.E2E.Tests.Tests;

[UITest]
public partial class StorageTests : BrowserTest
{
    private static readonly string[] StoragePaths = ["/storage-server", "/storage-wasm"];

    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task WebStorage_WorksInBothRenderModes()
    {
        foreach (var path in StoragePaths)
        {
            await NavigateAsync(path, "#save-storage");

            var value = path == "/storage-server" ? "server-value" : "wasm-value";
            await _page.Locator("#storage-value").FillAsync(value);
            await _page.Locator("#save-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync($"Saved: {value}");

            await _page.Locator("#storage-value").FillAsync(string.Empty);
            await _page.Locator("#load-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync($"Loaded: {value}");

            await _page.Locator("#remove-storage").ClickAsync();
            await _page.Locator("#load-storage").ClickAsync();
            await Expect(_page.Locator("#storage-result")).ToHaveTextAsync("Value is missing.");
        }
    }

    private async Task NavigateAsync(string path, string interactiveSelector)
    {
        await _page.GotoAsync($"{_server.TestUrl}{path}");
        await _page.WaitForInteractiveAsync(interactiveSelector);
    }
}
